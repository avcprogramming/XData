// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System.ComponentModel;
using static System.String;
using System.Collections.Generic;
using System.IO;
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
  /// Хранение настроек извлечения данных в XData таблицы. Для автоматического перезаполнения всех таблиц
  /// </summary>
  internal class
  XRecordTable
  {

    [Browsable(false)]
    public string
    RecordName => "AVCDataTable";

    private const char
    SamePathSymbol = '»';

    public const int
    MaxSelected = 3000;

    public const int
    MaxFiles = 100;

    /// <summary>
    /// Ключ реестра команды извлечения данных, заполнившей таблицу данных. 
    /// Включая номер стиля команды извлечения данных
    /// </summary>  
    public string
    StyleKey
    { get; set; }

    public DataSourceEnum
    DataSource
    { get; set; }

    /// <summary>
    /// Список файлов для случая DataSourceEnum.Files
    /// </summary>
    public List<string>
    SourceFiles
    { get; set; } 

    /// <summary>
    /// Список Id объектов, из которых извлечены данные для случая DataSourceEnum.Selected
    /// </summary>
    public List<ObjectId>
    SourceIds
    { get; set; }

    public bool
    HasSources => DataSource switch
    {
      DataSourceEnum.Files => SourceFiles is not null && SourceFiles.Count > 0,
      DataSourceEnum.Selected or DataSourceEnum.SelectView => SourceIds is not null && SourceIds.Count > 0,
      DataSourceEnum.NotLoaded => false,
      _ => true
    };

    public bool
    IsNull => IsNullOrEmpty(StyleKey) || DataSource == DataSourceEnum.NotLoaded;

    public ResultBuffer
    Buffer
    {
      get // создание нового буфера из свойств XRecordTable для записи в XRecord
      {
        ResultBuffer buffer = new();
        buffer.AddRecord(StyleKey);
        buffer.AddRecord((int)DataSource);
        if (DataSource == DataSourceEnum.Selected || DataSource == DataSourceEnum.SelectView)
        {
          if (SourceIds is not null && SourceIds.Count > 0 && SourceIds.Count <= MaxSelected)
            foreach (ObjectId id in SourceIds)
              buffer.AddRecord(id);
        }
        else if (DataSource == DataSourceEnum.Files)
        {
          if (SourceFiles is null || SourceFiles.Count == 0 || SourceFiles.Count > MaxFiles) 
            return buffer;
          string prevPath = ""; // для экономии размера xData храним путь только у первого файла из списка
          foreach (string file in SourceFiles)
          {
            string path = Path.GetFullPath(file);
            if (!IsNullOrWhiteSpace(path) && path == prevPath)
              buffer.AddRecord(SamePathSymbol + Path.GetFileName(file)); // обозначим, что у файла надо взять путь от предыдущего файла символом SamePathSymbol
            else
            {
              buffer.AddRecord(file);
              prevPath = Path.GetFullPath(file);
            }
          }
        }
        return buffer;
      }
      set // чтение данных из буфера в свойства XRecordTable
      {
        if (value is null) { Clear(); return; }
        TypedValue[] arr = value.AsArray();
        if (arr.Length >= 1 && arr[0].Value is string sk)
          StyleKey = sk;
        if (arr.Length >= 2 && arr[1].Value is int ds)
          DataSource = (DataSourceEnum)ds;
        SourceIds = null;
        SourceFiles = null;
        if (arr.Length <= 2) return;
        if (DataSource == DataSourceEnum.Selected || DataSource == DataSourceEnum.SelectView)
          for (int i = 2; i < arr.Length; i++)
          {
            if (arr[i].Value is ObjectId id && id.IsValid)
            {
              if (SourceIds is null) SourceIds = new();
              SourceIds.Add(id);
            }
          }
        else if (DataSource == DataSourceEnum.Files)
        {
          string prevPath = "";
          for (int i = 2; i < arr.Length; i++)
            if (arr[i].Value is string fn)
            {
              if (IsNullOrWhiteSpace(fn)) continue;
              if (SourceFiles is null) SourceFiles = new();
              if (fn[0] == SamePathSymbol)
                SourceFiles.Add(fn.Replace(SamePathSymbol.ToString(), prevPath));
              else
              {
                SourceFiles.Add(fn);
                prevPath = Path.GetFullPath(fn) + "\\";
              }
            }
        }
      }

    }

    public
    XRecordTable() : base()
    { DataSource = DataSourceEnum.NotLoaded; }

    /// <summary>
    /// Создание XRecordTable для записи в XRecord таблицы
    /// </summary>
    /// <param name="styleKey"></param>
    /// <param name="dataSource"></param>
    /// <param name="sourceFiles"></param>
    /// <param name="sourceIds"></param>
    public
    XRecordTable(string styleKey, DataSourceEnum dataSource, List<ObjectId> sourceIds, List<string> sourceFiles) : base()
    {
      StyleKey = styleKey;
      DataSource = dataSource;
      SourceFiles = sourceFiles;
      SourceIds = sourceIds;
    }

    /// <summary>
    /// Чтение XRecord из таблицы и сохранение данных в свойствах XRecordTable
    /// </summary>
    /// <param name="id"></param>
    public
    XRecordTable(ObjectId tableId, Transaction tr)
    {
      ReadFrom(tableId, tr);
    }

    /// <summary>
    /// Чтение XRecord из из таблицы и сохранение данных в свойствах 
    /// </summary>
    public
    XRecordTable(Table table, Transaction tr)
    {
      if (table is null) return;
      ReadFrom(table, tr);
    }


    public void
    Clear()
    {
      StyleKey = "";
      DataSource = DataSourceEnum.NotLoaded;
      SourceFiles = null;
      SourceIds = null;
    }

    /// <summary>
    /// Считывание данных из объекта чертежа
    /// </summary>
    public bool
    ReadFrom(DBObject obj, Transaction tr)
    {
      Clear();
      if (obj is null) return false;
      ObjectId dictId = obj.ExtensionDictionary;
      if (!dictId.IsValid) return false;

      DBDictionary dict = tr.GetObject(dictId, OpenMode.ForRead) as DBDictionary;
      if (dict is null) return false;
      ObjectId xrecId = ObjectId.Null;
      if (dict.Contains(RecordName)) xrecId = dict.GetAt(RecordName);
      if (!xrecId.IsValid) return false;
      Xrecord xrec = tr.GetObject(xrecId, OpenMode.ForRead) as Xrecord;
      if (xrec is null) return false;
      using ResultBuffer resBuf = xrec.Data;
      Buffer = resBuf;
      return !IsNull;
    }

    /// <summary>
    /// Считывание данных из объекта чертежа
    /// </summary>
    public bool
    ReadFrom(ObjectId id, Transaction tr)
    {
      Clear();
      if (id.IsNull || id.Database is null) return false;
      DBObject obj;
      try { obj = tr.GetObject(id, OpenMode.ForRead); }
      catch (Rt.Exception ex) { if (ex.ErrorStatus == Rt.ErrorStatus.PermanentlyErased) return false; else throw; }
      if (obj is null) return false;
      return ReadFrom(obj, tr);
    }

    /// <summary>
    /// Запись имеющихся данных в объекта чертежа.
    /// Вызывает RegApp, если объект сохранен в БД. Иначе надо вызывать RegApp заранее.
    /// Если объект был открыт для чтения - откроет для записи.
    /// </summary>
    public void
    SaveTo(DBObject obj, Transaction tr)
    {
      if (obj is null || obj.IsErased || obj.IsDisposed) return;
      if (!obj.IsWriteEnabled && !obj.ObjectId.IsNull)
        tr.GetObject(obj.ObjectId, OpenMode.ForWrite, false, true); // замена UpgradeOpen https://forums.autodesk.com/t5/net/api-bug-2018-1-causes-crash-using-upgradeopen-on-dependent/m-p/7272262/highlight/true
      ObjectId dictId = obj.ExtensionDictionary;
      if (dictId.IsNull)
      {
        obj.CreateExtensionDictionary();
        dictId = obj.ExtensionDictionary;
      }
      if (dictId.IsNull) return;
      DBDictionary dict = tr.GetObject(dictId, OpenMode.ForWrite) as DBDictionary;
      if (dict is null) return;
      using Xrecord xrec = new();
      using ResultBuffer resBuff = Buffer;
      if (dict.Contains(RecordName))
        dict.Remove(RecordName);
      xrec.Data = resBuff;
      dict.SetAt(RecordName, xrec);
    }

    /// <summary>
    /// Проверка наличия XRecord у объекта. Проверяется только наличие записи RecordName
    /// </summary>
    public bool
    HasXRecord(DBObject obj, Transaction tr)
    {
      if (obj is null) return false;
      ObjectId dictId = obj.ExtensionDictionary;
      if (!dictId.IsValid) return false;
      DBDictionary dict = tr.GetObject(dictId, OpenMode.ForRead) as DBDictionary;
      if (dict is null) return false;
      return dict.Contains(RecordName);
    }

    /// <summary>
    /// Очищает XRecord только для заданного RecordName. Пустой словарь уничтожается.
    /// </summary>
    public void
    ClearXRecord(DBObject obj, Transaction tr)
    {
      if (obj is null) return;
      ObjectId dictId = obj.ExtensionDictionary;
      if (!dictId.IsValid) return;
      DBDictionary dict = tr.GetObject(dictId, OpenMode.ForWrite) as DBDictionary;
      if (dict is null) return;
      if (dict.Contains(RecordName)) 
        dict.Remove(RecordName);
      if (dict.Count == 0)
      {
        if (!obj.IsWriteEnabled && !obj.ObjectId.IsNull)
          tr.GetObject(obj.ObjectId, OpenMode.ForWrite, false, true); // замена UpgradeOpen https://forums.autodesk.com/t5/net/api-bug-2018-1-causes-crash-using-upgradeopen-on-dependent/m-p/7272262/highlight/true
        obj.ReleaseExtensionDictionary();
      }
    }

    public override string
    ToString() => IsNull || !HasSources ? "Null" : 
      $"{StyleKey.Replace("AVC_","").Replace("2020","")}, {DataSource}" + (DataSource switch 
        { 
          DataSourceEnum.Files => $" ({SourceFiles?.Count})", 
          DataSourceEnum.Selected or DataSourceEnum.SelectView => $" ({SourceIds?.Count})",
          _ => ""
        });

  } 
}
