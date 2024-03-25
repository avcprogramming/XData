// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

// Ignore Spelling: Bandings

using System;
using System.Collections.Generic;
#if BRICS
using Teigha.DatabaseServices;
using CadApp = Bricscad.ApplicationServices.Application;
using Rt = Teigha.Runtime;
#else
using Autodesk.AutoCAD.DatabaseServices;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Rt = Autodesk.AutoCAD.Runtime;
#endif

namespace AVC
{

  /// <summary>
  /// Стороны прямоугольной детали на выкладке. 
  /// Для удобства индексирования подстановок int значения сдвинуты на 1000
  /// (исходя из того что у детали не может быть больше чем 999 торцев)
  /// </summary>
  internal enum
  EdgeSide
  {
    Other = 0,
    Left = 1000,
    Top = 2000,
    Right = 3000,
    Bottom = 4000
  }

  /// <summary>
  /// Хранит список идентификаторов, длин, углов пила и площадей всех поверхностей солида, смежных с фасадной поверхностью. 
  /// Материал покрытия следует получать из самого солида - считанные из xData AvcSolidBanding не содержат информацию о материале.
  /// </summary>
  internal class 
  XDataBandings : XDataMan
  {

    public List<AvcSolidBanding> 
    Bandings = new();

    public ObjectId
    OwnerId;

    public override string 
    XDAppName => "AVCBandings";

    public 
    XDataBandings() : base()
    { }

    public 
    XDataBandings(List<AvcSolidBanding> bandings) : base()
    { 
      if (bandings is null) return; 
      Bandings = bandings;
      if (bandings is not null && bandings.Count > 0)
        OwnerId = bandings[0].OwnerId;
    }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в Bandings. Материалы не считываются  
    /// </summary>
    /// <param name="id"></param>
    public 
    XDataBandings(ObjectId id, Transaction tr)
    {
      OwnerId = id;
      ReadFrom(id, tr);
    }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в Bandings. Материалы не считываются 
    /// </summary>
    /// <param name="id"></param>
    public 
    XDataBandings(DBObject obj)
    {
      if (obj is null) return;
      OwnerId = obj.Id;
      ReadFrom(obj);
    }

    public const int 
    BufferLength = 6;

    /// <summary>
    /// Для записи xData используем DBObject.XData = Buffer, для чтения Buffer = DBObject.GetXDataForApplication(XDAppName)
    /// </summary>
    public override ResultBuffer 
    Buffer
    {
      get
      {
        ResultBuffer buffer = NewXData(); // Первое поле - AppName
        foreach (AvcSolidBanding banding in Bandings)
        {
          buffer.AddVal(banding.IntId);
          buffer.AddVal(banding.Area);
          buffer.AddVal(banding.Length);
          buffer.AddVal(banding.Angle);
          buffer.AddVal(banding.Letter);
          buffer.AddVal(banding.SideIndex);
        }
        return buffer;
      }
      set
      {
        Clear();
        if (value is null) return;
        TypedValue[] arr = value.AsArray();
        
        for (int i = 1; i < arr.Length; i += BufferLength)
        {
          AvcSolidBanding banding = new() { OwnerId = OwnerId };
          if (arr.Length >= i + 1 && arr[i].Value is int id) banding.IntId = id; else break;
          if (arr.Length >= i + 2 && arr[i + 1].Value is double area) banding.Area = area; else break;
          if (arr.Length >= i + 3 && arr[i + 2].Value is double length) banding.Length = length; else break;
          if (arr.Length >= i + 4 && arr[i + 3].Value is double angle) banding.Angle = angle; else break;
          if (arr.Length >= i + 5 && arr[i + 4].Value is string letter) banding.Letter = letter; else break;
          if (arr.Length >= i + 6 && arr[i + 5].Value is int sideIndex)
          {
            banding.Side = GetSide(sideIndex);
            banding.Index = GetIndex(sideIndex); 
          }
          else break;
          Bandings.Add(banding);
        }

      }
    }

    public override void 
    Clear()
    {
      Bandings.Clear();
    }

    // ================================== EdgeSide ================================================================
    #region EdgeSide static

    private static readonly Type
    _edgeSideType = typeof(EdgeSide);

    /// <summary>
    /// Число - один спец-индексов направления торца EdgeSide
    /// </summary>
    public static bool
    IsSide(int sideIndex) => _edgeSideType.IsEnumDefined(sideIndex);

    /// <summary>
    /// Преобразовать специальные индексы торцев в направления
    /// </summary>
    /// <param name="index">Специальные индексы торцев</param>
    /// <returns></returns>
    public static EdgeSide
    GetSide(int sideIndex)
    {
      int side = sideIndex - sideIndex % 1000;
      return IsSide(side) ? (EdgeSide)side : EdgeSide.Other;
    }
    
    /// <summary>
    /// Преобразовать специальные индексы торцев в чистый индекс
    /// </summary>
    /// <param name="index">Специальные индексы торцев</param>
    /// <returns></returns>
    public static int
    GetIndex(int sideIndex) => sideIndex % 1000;

    /// <summary>
    /// Замена направлений, как делает LAY для деталей с текстуров поперек
    /// </summary>
    /// <param name="old">исходное направление у детали выложенной для обмера, 
    /// но не на окончательной выкладке (длинная сторона по X)</param>
    /// <returns>повернутое на 90 градусов против часовой</returns>
    public static EdgeSide
    Rotate90(EdgeSide old) => old switch
    {
      EdgeSide.Left => EdgeSide.Bottom,
      EdgeSide.Top => EdgeSide.Left,
      EdgeSide.Right => EdgeSide.Top,
      EdgeSide.Bottom => EdgeSide.Right,
      _ => EdgeSide.Other
    };

    /// <summary>
    /// Замена направлений, как делает LAY для деталей, помеченных как зеркальные
    /// </summary>
    /// <param name="old">направление на реальной детали</param>
    /// <returns>заменены лево и право</returns>
    public static EdgeSide
    MirrorX(EdgeSide old) => old switch
    {
      EdgeSide.Left => EdgeSide.Right,
      EdgeSide.Right => EdgeSide.Left,
      _ => old
    };

    #endregion

  }
}
