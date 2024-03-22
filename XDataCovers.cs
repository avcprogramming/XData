// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

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
  /// <summary>
  /// Хранит список идентификаторов и площадей всех поверхностей солида, которые имеют покрытия. 
  /// Материал покрытия следует получать из самого солида - считанные из xData AvcSolidFace не содержат информацию о материале.
  /// </summary>
  internal class 
  XDataCovers : XDataMan
  {
    public List<AvcSolidFace> 
    Covers = new();

    public ObjectId
    OwnerId
    { get; private set; }

    public override string 
    XDAppName => "AVCCovers";

    public 
    XDataCovers() : base()
    { }

    public 
    XDataCovers(List<AvcSolidFace> covers) : base()
    { 
      if (covers is null) return;  
      Covers = covers;
      if (covers is not null && covers.Count > 0)
        OwnerId = covers[0].OwnerId;
    }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в свойствах 
    /// </summary>
    /// <param name="id"></param>
    public 
    XDataCovers(ObjectId id, Transaction tr) 
    {
      OwnerId = id;
      ReadFrom(id, tr);
    }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в свойствах 
    /// </summary>
    /// <param name="id"></param>
    public 
    XDataCovers(DBObject obj) 
    {
      if (obj is null) return;
      OwnerId = obj.Id;
      ReadFrom(obj);
    }

    /// <summary>
    /// Для записи xData используем DBObject.XData = Buffer, для чтения Buffer = DBObject.GetXDataForApplication(XDAppName)
    /// </summary>
    public override ResultBuffer 
    Buffer
    {
      get
      {
        ResultBuffer buffer = NewXData(); // Первое поле - AppName
        foreach (AvcSolidFace cover in Covers)
        {
          buffer.AddVal((int)cover.Dir);
          buffer.AddVal(cover.IntId);
          buffer.AddVal(cover.Area);
        }
        return buffer;
      }
      set
      {
        Clear();
        if (value is null) return;
        TypedValue[] arr = value.AsArray();
        for (int i = 1; i < arr.Length; i += 3)
        {
          AvcSolidFace cover = new() { OwnerId = OwnerId };
          if (arr.Length >= i + 1 && arr[i].Value is int dir) cover.Dir = (AvcSolidFace.Direction)dir; else break;
          if (arr.Length >= i + 2 && arr[i+1].Value is int id) cover.IntId = id; else break;
          if (arr.Length >= i + 3 && arr[i+2].Value is double area) cover.Area = area; else break;
          Covers.Add(cover);
        }

      }
    }

    public override void 
    Clear()
    {
      Covers.Clear();
    }

  }
}
