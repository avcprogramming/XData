// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System.Collections.Generic;
using System.ComponentModel;
#if BRICS
using Teigha.DatabaseServices;
#else
using Autodesk.AutoCAD.DatabaseServices;
#endif

namespace AVC
{
  internal class 
  XDataUserProperty : XDataMan
  {
    readonly string 
    _appName;

    public const string 
    fieldNamePrefix = "AVCUserProperty";

    [Browsable(false)]
    public override string 
    XDAppName => fieldNamePrefix + _appName; 

    public string 
    Value { get; set; }

    public 
    XDataUserProperty(string propName, string value) : base()
    { _appName = DatabaseExt.ValidName(propName); Value = value ?? ""; }

    public 
    XDataUserProperty(KeyValuePair<string, string> property) : base()
    { _appName = DatabaseExt.ValidName(property.Key); Value = property.Value ?? ""; }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в свойствах 
    /// </summary>
    /// <param name="id"></param>
    public 
    XDataUserProperty(string propName, ObjectId id, Transaction tr) : base()
    {
      _appName = DatabaseExt.ValidName(propName);
      ReadFrom(id, tr);
    }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в свойствах 
    /// </summary>
    /// <param name="id"></param>
    public 
    XDataUserProperty(string propName, DBObject obj) : base()
    {
      _appName = DatabaseExt.ValidName(propName);
      if (obj is null) return;
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
        buffer.AddVal(Value); 
        return buffer;
      }
      set
      {
        if (value is null) { Clear(); return; }
        TypedValue[] arr = value.AsArray();
        if (arr.Length >= 2 && arr[1].Value is string s2) Value = s2;
      }
    }

    public override void 
    Clear() 
    {
      Value = "";
    }

  }
}
