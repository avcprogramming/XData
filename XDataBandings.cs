// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

// Ignore Spelling: Bandings

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
#if BRICS
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Bricscad.EditorInput;
using Teigha.Geometry;
using Teigha.Runtime;
using CadApp = Bricscad.ApplicationServices.Application;
using Rt = Teigha.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using Autodesk.AutoCAD.Runtime;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Rt = Autodesk.AutoCAD.Runtime;
#endif

namespace AVC
{
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
          buffer.AddVal(banding.Index);
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
          AvcSolidBanding banding = new() { OwnerId = OwnerId, Index = i / BufferLength + 1 };
          if (arr.Length >= i + 1 && arr[i].Value is int id) banding.IntId = id; else break;
          if (arr.Length >= i + 2 && arr[i + 1].Value is double area) banding.Area = area; else break;
          if (arr.Length >= i + 3 && arr[i + 2].Value is double length) banding.Length = length; else break;
          if (arr.Length >= i + 4 && arr[i + 3].Value is double angle) banding.Angle = angle; else break;
          if (arr.Length >= i + 5 && arr[i + 4].Value is string letter) banding.Letter = letter; else break;
          if (arr.Length >= i + 6 && arr[i + 5].Value is int index) banding.Index = index; else break;
          Bandings.Add(banding);
        }

      }
    }

    public override void 
    Clear()
    {
      Bandings.Clear();
    }

  }
}
