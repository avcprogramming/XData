// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System.ComponentModel;
using System;
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
using Rt =  Autodesk.AutoCAD.Runtime;
#endif

namespace AVC
{
  /// <summary>
  /// Хранение имен солидов и др объектов чертежа в XData
  /// </summary>
  internal class 
  XDataTbSplit : XDataMan
  {

    [Browsable(false)]
    public override string 
    XDAppName { get { return "AVCTbSplit"; } }

    [Browsable(false)]
    public Guid 
    Guid { get; set; }

    public int 
    Section { get; set; } // номер раздела с 1

    public bool 
    IsNull => Guid == Guid.Empty; 


    public 
    XDataTbSplit() : base()
    { }

    public 
    XDataTbSplit(Guid guid, int section) : base()
    {
      Guid = guid; Section = section; 
    }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в свойствах 
    /// </summary>
    /// <param name="id"></param>
    public 
    XDataTbSplit(ObjectId id, Transaction tr) : base(id, tr)
    { }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в свойствах 
    /// </summary>
    /// <param name="id"></param>
    public 
    XDataTbSplit(DBObject obj) : base(obj)
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
        buffer.AddVal(Guid);
        buffer.AddVal(Section);
        return buffer;
      }
      set
      {
        if (value is null) { Clear(); return; }
        TypedValue[] arr = value.AsArray();
        if (arr.Length >= 2 && arr[1].Value is string s)
          Guid = new Guid(s);
        if (arr.Length >= 3 && arr[2].Value is int i)
          Section = i;
      }
      
    }

    public override void 
    Clear()
    {
      Guid = Guid.Empty;
      Section = 0;
    }

    /// <summary>
    /// очистить метки всех разделенных таблиц в блоке BTRId
    /// </summary>
    /// <param name="BTRId"></param>
    /// <param name="tr"></param>
    public void 
    ClearAllTables(ObjectId BTRId, Transaction tr)
    {
      if (BTRId.IsNull || BTRId.IsErased) return;
      BlockTableRecord btr = tr.GetObject(BTRId, OpenMode.ForRead) as BlockTableRecord;
      if (btr is null) return;
      foreach (ObjectId id in btr)
        if (!id.IsNull && !id.IsErased && id.IsValid && id.IsTable())
          using (Table tb = tr.GetObject(id, OpenMode.ForRead) as Table)
            if (tb != null)
            {
              XDataTbSplit xD = new(tb);
              if (!xD.IsNull) xD.ClearXData(tb, tr);
            }
    }

  }
}
