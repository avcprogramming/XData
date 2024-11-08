// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System;
using System.Threading;
using System.ComponentModel;
#if BRICS
using Teigha.DatabaseServices;
#else
using Autodesk.AutoCAD.DatabaseServices;
#endif

namespace AVC
{
  /// <summary>
  /// набор свойств объектов чертежа типа ДА/НЕТ
  /// </summary>
  [Flags]
  internal enum
  SolidFlags
  {
    None = 0,
    /// <summary>
    /// Для солидов - обмерять как развертку
    /// </summary>
    Sweep = 1,
    /// <summary>
    /// Для подсчета количества одинаковых деталей - считать отдельно как зеркальные
    /// </summary>
    Mirror = 2,
    /// <summary>
    /// Для солидов - имеет направление волокон, текстуру
    /// </summary>
    Textured = 8,
    /// <summary>
    /// Для солидов - текстура поперек, по короткой стороне
    /// </summary>
    Across = 16
  }

  internal enum
  TextureAlong
  {
    Indeterminate = -1,
    No = 0,
    Along = SolidFlags.Textured,
    Across = SolidFlags.Textured + SolidFlags.Across
  };

  /// <summary>
  /// Хранение имен солидов и др объектов чертежа в XData
  /// </summary>
  internal class
  XDataNames : XDataMan
  {

    [Browsable(false)]
    public override string
    XDAppName=> "AVCNames"; 

    /// <summary>
    /// Строковое имя объекта чертежа. Может содержать маску формата размеров солида для SolidMetric.ToString(Format) 
    /// </summary>
    public string
    Name
    { get; set; }

    public SolidFlags
    Flags
    { get; set; }

    public string
    Kind
    { get; set; }

    public string
    Info
    { get; set; }

    /// <summary>
    /// Для подсчета количества одинаковых деталей - считать отдельно как зеркальные
    /// </summary>
    public bool
    Mirror
    {
      get { return Flags.HasFlag(SolidFlags.Mirror); }
      set
      {
        if (value)
          Flags |= SolidFlags.Mirror;
        else
          Flags &= ~SolidFlags.Mirror;
      }
    }

    /// <summary>
    /// Для солидов - обмерять как развертку
    /// </summary>
    public bool
    Sweep
    {
      get { return Flags.HasFlag(SolidFlags.Sweep); }
      set
      {
        if (value)
          Flags |= SolidFlags.Sweep;
        else
          Flags &= ~SolidFlags.Sweep;
      }
    }

    /// <summary>
    /// Для солидов - имеет направление волокон, текстуру
    /// </summary>
    public bool
    Textured
    {
      get { return Flags.HasFlag(SolidFlags.Textured); }
      set
      {
        if (value)
          Flags |= SolidFlags.Textured;
        else
          Flags &= ~SolidFlags.Textured;
      }
    }

    /// <summary>
    /// Для солидов - текстура поперек, по короткой стороне
    /// </summary>
    public bool
    Across
    {
      get { return Flags.HasFlag(SolidFlags.Across); }
      set
      {
        if (value)
          Flags |= SolidFlags.Across;
        else
          Flags &= ~SolidFlags.Across;
      }
    }

    /// <summary>
    /// Для солидов - текстура
    /// </summary>
    public TextureAlong
    Texture
    {
      get { return (TextureAlong)(Flags & (SolidFlags.Textured | SolidFlags.Across)); }
      set
      {
        Flags &= ~(SolidFlags.Textured | SolidFlags.Across);
        if (value == TextureAlong.Along)
          Flags |= SolidFlags.Textured;
        else if (value == TextureAlong.Across)
          Flags |= SolidFlags.Textured | SolidFlags.Across;
      }
    }

    public
    XDataNames() : base()
    { }

    public
    XDataNames(string Name, SolidFlags Flags, string Kind = "", string Info = "") : base()
    { this.Name = Name; this.Flags = Flags; this.Info = Info; this.Kind = Kind; }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в свойствах 
    /// </summary>
    /// <param name="id"></param>
    public
    XDataNames(ObjectId id, Transaction tr) : base(id, tr)
    { }

    /// <summary>
    /// Чтение xData из объекта чертежа и сохранение данных в свойствах 
    /// </summary>
    /// <param name="id"></param>
    public
    XDataNames(DBObject obj) : base(obj)
    { }

    /// <summary>
    /// Для записи xData используем DBObject.XData = Buffer, для чтения Buffer = DBObject.GetXDataForApplication(XDAppName)
    /// </summary>
    public override ResultBuffer
    Buffer
    {
      get
      {
        ResultBuffer buffer = base.NewXData(); // Первое поле - AppName
        buffer.AddData(Name); // Второе - Имя
        buffer.AddData((int)Flags); // Третье - Флаги
        buffer.AddData(Kind);
        buffer.AddData(Info);
        return buffer;
      }
      set
      {
        if (value is null) { Clear(); return; }
        TypedValue[] arr = value.AsArray();
        if (arr.Length >= 2 && arr[1].Value is string s2) Name = s2;
        if (arr.Length >= 3 && arr[2].Value is int) Flags = (SolidFlags)arr[2].Value;
        if (arr.Length >= 4 && arr[3].Value is string s4) Kind = s4;
        if (arr.Length >= 5 && arr[4].Value is string s5) Info = s5;
      }
    }

    public override void
    Clear()
    {
      Name = "";
      Flags = SolidFlags.None;
      Kind = "";
      Info = "";
    }

    /// <summary>
    /// Инверсия одного бита в SubFlags
    /// </summary>
    /// <param name="bit"></param>
    /// <returns>результирующее значение этого бита после инверсии</returns>
    public bool
    InvertFlag(SolidFlags bit)
    {
      if ((Flags & bit) != 0) { Flags &= ~bit; return false; }
      else { Flags |= bit; return true; }
    }

#if VARS
    /// <summary>
    /// ARX плагин AVC_Names|Avc_Palette загружен
    /// </summary>
    public static bool
    AVCNamesLoaded
    {
      get
      {
        try
        {
          Mutex InstanceMutex = Mutex.OpenExisting("AVC_Names", System.Security.AccessControl.MutexRights.ReadPermissions);
          return InstanceMutex != null;
        }
        catch (WaitHandleCannotBeOpenedException) { return false; }
        catch (UnauthorizedAccessException) { return false; }
      }
    }
#endif

  }
}
