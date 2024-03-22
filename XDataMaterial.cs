// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System;
using System.Collections.Generic;
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

  [Flags]
  internal enum 
  MaterialEnum
  {
    Grain = 1, // материал имеет текстуру вдоль длинной стороны
  }

  /// <summary>
  /// Хранение данных о материале в XData объектов Material
  /// </summary>
  internal class 
  XDataMaterial : XDataMan
  {
    public override string XDAppName { get { return "AVCMaterial"; } }

    public MatUseLike 
    Use { get; set; }

    public double 
    Density { get; set; } // плотность на кубометр/кубический дюйм

    public double 
    Length { get; set; }

    public double 
    Width { get; set; }

    public double 
    Thickness { get; set; }

    public string 
    Index { get; set; }

    public string 
    Article { get; set; }

    public double 
    Price { get; set; } // цена материала в зависимости от Use - за погонный, квадратный или кубический метр (в миллиметровом череже, в прочих - в текущих единицах)

    public MaterialEnum 
    Flags { get; set; }

    public string 
    MillTool { get; set; }

    public string 
    MillMode { get; set; }

    public string 
    SawTool { get; set; }

    public string 
    SawMode { get; set; }


    public 
    XDataMaterial() : base()
    { }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в свойствах 
    /// </summary>
    /// <param name="id"></param>
    public 
    XDataMaterial(ObjectId id, Transaction tr) : base(id, tr)
    { }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в свойствах 
    /// </summary>
    /// <param name="id"></param>
    public 
    XDataMaterial(DBObject obj) : base(obj)
    { }

    /// <summary>
    /// Для записи xData используем DBObject.XData = Buffer, для чтения Buffer = DBObject.GetXDataForApplication(XDAppName)
    /// </summary>
    public override ResultBuffer 
    Buffer
    {
      get
      {
        ResultBuffer buffer = NewXData(); // Первое поле - AppName
        buffer.AddVal((int)Use);
        buffer.AddVal(Density);
        buffer.AddVal(Length);
        buffer.AddVal(Width);
        buffer.AddVal(Thickness);
        buffer.AddVal(Index);
        buffer.AddVal(Article);
        buffer.AddVal(Price);
        buffer.AddVal((int)Flags);
        buffer.AddVal(MillTool);
        buffer.AddVal(MillMode);
        buffer.AddVal(SawTool);
        buffer.AddVal(SawMode);
        return buffer;
      }
      set
      {
        if (value is null) { Clear(); return; }
        TypedValue[] arr = value.AsArray();
        if (arr.Length >= 2 && arr[1].Value is int i1) Use = (MatUseLike)i1;
        if (arr.Length >= 3 && arr[2].Value is double d2) Density = d2;
        if (arr.Length >= 4 && arr[3].Value is double d3) Length = d3;
        if (arr.Length >= 5 && arr[4].Value is double d4) Width = d4;
        if (arr.Length >= 6 && arr[5].Value is double d5) Thickness = d5;
        if (arr.Length >= 7 && arr[6].Value is string s6) Index = s6;
        if (arr.Length >= 8 && arr[7].Value is string s7) Article = s7;
        if (arr.Length >= 9 && arr[8].Value is double d8) Price = d8;
        if (arr.Length >= 10 && arr[9].Value is int i9) Flags = (MaterialEnum)i9;
        if (arr.Length >= 11 && arr[10].Value is string s10) MillTool = s10;
        if (arr.Length >= 12 && arr[11].Value is string s11) MillMode = s11;
        if (arr.Length >= 13 && arr[12].Value is string s12) SawTool = s12;
        if (arr.Length >= 14 && arr[13].Value is string s13) SawMode = s13;
      }
    }

    public override void 
    Clear()
    {
      Use = MatUseLike.Volume;
      Density = 0;
      Length = 0;
      Width = 0;
      Thickness = 0;
      Index = "";
      Article = "";
      Price = 0;
      Flags = 0;
      MillTool = "";
      MillMode = "";
      SawTool = "";
      SawMode = "";
    }

  }
}
