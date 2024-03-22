// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/

using System.ComponentModel;
using System;
#if BRICS
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using Teigha.Geometry;
using CadApp = Bricscad.ApplicationServices.Application;
using Db = Teigha.DatabaseServices;
using Rt = Teigha.Runtime;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
using Db = Autodesk.AutoCAD.DatabaseServices;
using Rt = Autodesk.AutoCAD.Runtime;
#endif

namespace AVC
{
  /// <summary>
  /// Работа с DBObject.XData
  /// </summary>
  internal class
  XDataMan
  {
    [Browsable(false)]
    public virtual string
    XDAppName
    { get; private set; } // надо перегрузить у потомков!

    /// <summary>
    /// Надо переопределить у наследников чтоб хранить не буфер, а конкретные свойства.
    /// Для записи xData используем DBObject.XData = Buffer, для чтения Buffer = DBObject.GetXDataForApplication(XDAppName)
    /// </summary>
    [Browsable(false)]
    public virtual ResultBuffer
    Buffer
    { get; set; }

    public
    XDataMan()
    {
    }

    public
    XDataMan(ObjectId id, Transaction tr)
    {
      ReadFrom(id, tr);
    }

    public
    XDataMan(DBObject obj)
    {
      if (obj is null) return;
      ReadFrom(obj);
    }

    /// <summary>
    /// Очистка полей данных. Надо переопределить у наследников
    /// </summary>
    public virtual void
    Clear()
    { }

    /// <summary>
    /// Считывание данных из объекта чертежа
    /// </summary>
    public bool
    ReadFrom(DBObject obj)
    {
      Clear();
      if (obj is null) return false;
      using ResultBuffer ret = obj.GetXDataForApplication(XDAppName);
      if (ret is null) return false;
      Buffer = ret; // наследники должны переопределить буфер так чтоб данные хранились в нормальных полях и буфер можно было уничтожить
      return true;
    }

    /// <summary>
    /// Считывание данных из объекта чертежа
    /// При необходимости Создает транзакцию
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
      return ReadFrom(obj);
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
      RegApp(obj.Database, tr);
      if (!obj.IsWriteEnabled && !obj.ObjectId.IsNull)
        tr.GetObject(obj.ObjectId, OpenMode.ForWrite, false, true); // замена UpgradeOpen https://forums.autodesk.com/t5/net/api-bug-2018-1-causes-crash-using-upgradeopen-on-dependent/m-p/7272262/highlight/true
      obj.XData = Buffer;
    }

    /// <summary>
    /// Запись имеющихся данных в объекта чертежа
    /// При необходимости Создает транзакцию
    /// Вызывает RegApp
    /// </summary>
    public void
    SaveTo(ObjectId id, Transaction tr)
    {
      if (id.IsNull || id.Database is null) return;
      DBObject obj = tr.GetObject(id, OpenMode.ForWrite, false, true);
      if (obj != null)
      {
        RegApp(id.Database, tr);
        obj.XData = Buffer;
      }
    }


    /// <summary>
    /// Регистрация приложения для использования XData в заданной базе данных чертежа. Если db не задана - используется текущий чертеж
    /// </summary>
    public bool
    RegApp(Database db, Transaction tr)
    {
      if (string.IsNullOrWhiteSpace(XDAppName)) return false;
      if (db is null) return RegApp(tr);
      RegAppTable rTbl = tr.GetObject(db.RegAppTableId, OpenMode.ForRead) as RegAppTable;
      if (!rTbl.Has(XDAppName))
      {
        RegAppTableRecord xdRec = new();
        xdRec.Name = XDAppName;
        tr.GetObject(db.RegAppTableId, OpenMode.ForWrite);
        rTbl.Add(xdRec);
        tr.AddNewlyCreatedDBObject(xdRec, true);
      }
      return true;
    }

    /// <summary>
    /// Регистрация приложения в текущем документе для использования XData
    /// Создает транзакцию и блокирует текущий чертеж
    /// </summary>
    public bool
    RegApp(Transaction tr)
    {
      if (string.IsNullOrWhiteSpace(XDAppName)) return false;
      Document doc = CadApp.DocumentManager.MdiActiveDocument;
      if (doc is null) return false;
      Database db = doc.Database;
      using (doc.LockIfNone("XDAppName"))
        return RegApp(db, tr);
    }

    /// <summary>
    /// Получить все значения xData в виде массива. В ячейке 0 будет имя приложения
    /// </summary>
    public TypedValue[]
    GetArray(DBObject obj)
    {
      using ResultBuffer rb = obj.GetXDataForApplication(XDAppName);
      if (rb != null) return rb.AsArray();
      else return new TypedValue[1];
    }

    /// <summary>
    /// получить одно значение из xData. можно использовать когда IsNull == true
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="index">нулевой элемент - это имя приложения</param>
    public object
    GetValue(DBObject obj, int index)
    {
      if (obj.XData is null) return null;
      using ResultBuffer buffer = obj.GetXDataForApplication(XDAppName);
      if (buffer is null) return null;
      if (buffer.AsArray().GetUpperBound(0) < index) return null;
      return buffer.AsArray()[index].Value;
    }

    /// <summary>
    /// Заменить одно из значений xData. остальные значения сохраняются. 
    /// Ничего не делает, если нет xData у объекта
    /// Работает только с int и string
    /// Вызывает RegApp
    /// </summary>
    /// <returns>успех</returns>
    public bool
    SetValue(DBObject obj, int index, object value, Transaction tr)
    {
      if (obj.XData is null) return false;
      using ResultBuffer buffer = obj.GetXDataForApplication(XDAppName);
      if (buffer is null) return false;
      TypedValue[] arr = buffer.AsArray();
      if (arr.GetUpperBound(0) < index) return false;
      if (value is int)
        arr[index] = new TypedValue((int)DxfCode.ExtendedDataInteger32, value);
      else
        arr[index] = new TypedValue((int)DxfCode.ExtendedDataAsciiString, value.ToString());
      if (!obj.IsWriteEnabled && !obj.ObjectId.IsNull)
        tr.GetObject(obj.ObjectId, OpenMode.ForWrite, false, true); // замена UpgradeOpen https://forums.autodesk.com/t5/net/api-bug-2018-1-causes-crash-using-upgradeopen-on-dependent/m-p/7272262/highlight/true
      RegApp(obj.Database, tr);
      obj.XData = new ResultBuffer(arr);
      return true;
    }

    /// <summary>
    /// Очищает xData только для заданного приложения, остальные xData сохраняются
    /// При необходимости Создает транзакцию и блокирует текущий чертеж
    /// Вызывает RegApp
    /// </summary>
    public void
    ClearXData(ObjectId objId, Transaction tr)
    {
      if (objId.IsNull || objId.IsErased || !objId.IsValid) return;
      DBObject obj = tr.GetObject(objId, OpenMode.ForWrite, false, true);
      if (obj is null) return;
      ClearXData(obj, tr);
    }

    /// <summary>
    /// Очищает xData только для заданного приложения, остальные xData сохраняются
    /// Вызывает RegApp
    /// </summary>
    public void
    ClearXData(DBObject obj, Transaction tr)
    {
      ResultBuffer rb = obj.GetXDataForApplication(XDAppName);
      if (rb != null)
      {
        if (!obj.IsWriteEnabled && !obj.ObjectId.IsNull)
          tr.GetObject(obj.ObjectId, OpenMode.ForWrite, false, true); // замена UpgradeOpen https://forums.autodesk.com/t5/net/api-bug-2018-1-causes-crash-using-upgradeopen-on-dependent/m-p/7272262/highlight/true
        RegApp(obj.Database, tr);
        obj.XData = NewXData();
        rb.Dispose();
      }
    }

    /// <summary>
    /// Проверка наличия xData у объекта. Проверяется только наличие XDAppName
    /// RegApp должен быть вызван заранее
    /// </summary>
    public bool
    HasXData(DBObject obj)
    {
      if (obj is null) return false;
      using ResultBuffer arr = obj.GetXDataForApplication(XDAppName);
      return (arr != null);
    }

    /// <summary>
    /// Проверка наличия xData у объекта. Проверяется только наличие XDAppName
    /// Использует существующую транзакцию, если она была создана до вызова
    /// </summary>
    public bool
    HasXData(ObjectId objId, Transaction tr) => HasXData(tr.GetObject(objId, OpenMode.ForRead));


    public ResultBuffer
    NewXData() => new(new TypedValue((int)DxfCode.ExtendedDataRegAppName, XDAppName));


    public override int
    GetHashCode()
    {
      ResultBuffer b = Buffer;
      if (b is null) return base.GetHashCode();
      return b.GetHashCode();
    }

    public override bool
    Equals(object obj)
    {
      ResultBuffer b = Buffer;
      if (b is null) return base.Equals(obj); // тупое сравнение адресов объектов
      if (obj is XDataMan xd)
      {
#if BRICS // хотя Equals переопределен у ResultBuffer, но не работает, всегда true
        ResultBuffer other = xd.Buffer;
        if (other is null) return false;
      TypedValue[] arr1 = b.AsArray();
        TypedValue[] arr2 = other.AsArray();
        if (arr1.Length != arr2.Length) return false;
        for (int i = 0; i < arr1.Length; i++)
          if (arr1[i].TypeCode != arr2[i].TypeCode || arr1[i].Value.ToString() != arr2[i].Value.ToString()) // TypedValue.Equals в BricsCAD тоже не работает - всегда false. А сравнивать Value нельзя даже в Автокаде - всегда false. Приходится приводить к строке 
            return false;
        return true;
#else
        return Buffer.Equals(xd.Buffer);
#endif
      }
      return false;
    }

    public static bool
    operator ==(XDataMan a, XDataMan b)
    {
      if (a is null) return b is null;
      return a.Equals(b);
    }

    public static bool
    operator !=(XDataMan a, XDataMan b) => !(a == b);


  }

  internal static class
  ResultBufferExt
  {
    public static void
    AddVal(this ResultBuffer rb, int value)
    {
      rb.Add(new TypedValue((int)DxfCode.ExtendedDataInteger32, value));
    }

    public static void
    AddVal(this ResultBuffer rb, string value)
    {
      rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, value));
    }

    public static void
    AddVal(this ResultBuffer rb, double value)
    {
      rb.Add(new TypedValue((int)DxfCode.ExtendedDataReal, value));
    }

    public static void
    AddVal(this ResultBuffer rb, Point3d value)
    {
      rb.Add(new TypedValue((int)DxfCode.ExtendedDataXCoordinate, value));
    }

    public static void
    AddVal(this ResultBuffer rb, double x, double y, double z)
    {
      rb.AddVal(new Point3d(x, y, z));
    }

    public static void
    AddVal(this ResultBuffer rb, byte[] value)
    {
      rb.Add(new TypedValue((int)DxfCode.ExtendedDataBinaryChunk, value));
    }

    public static void
    AddVal(this ResultBuffer rb, Matrix3d value)
    {
      double[] matrix4x3 = new double[]{
          value[0, 0], value[0, 1], value[0, 2], value[0, 3],
          value[1, 0], value[1, 1], value[1, 2], value[1, 3],
          value[2, 0], value[2, 1], value[2, 2], value[2, 3] }; // в четвертой строке всегда 0,0,0,1 - нет смысла сохранять
      byte[] result = new byte[4 * 3 * sizeof(double)];
      System.Buffer.BlockCopy(matrix4x3, 0, result, 0, result.Length);
      rb.AddVal(result);
    }

    public static void
    AddVal(this ResultBuffer rb, ObjectId value)
    {
      rb.Add(new TypedValue((int)DxfCode.ExtendedDataHandle, value.Handle));
    }

    public static void
    AddVal(this ResultBuffer rb, Guid value)
    {
      rb.Add(new TypedValue((int)DxfCode.ExtendedDataAsciiString, value.ToString()));
    }

    public static ObjectId
    GetObjectId(this TypedValue[] arr, int index, Database db)
    {
      if (arr.Length >= index + 1 && arr[index].Value is Handle handle && db.TryGetObjectId(handle, out ObjectId id))
        return id;
      return ObjectId.Null;
    }

    public static Matrix3d
    GetMatrix(this TypedValue[] arr, int index)
    {
      if (arr.Length >= index + 1 && arr[index].Value is byte[] bytes && bytes.Length == 4 * 3 * sizeof(double))
      {
        double[] result = new double[4 * 4];
        System.Buffer.BlockCopy(bytes, 0, result, 0, bytes.Length);
        result[12] = 0.0; result[13] = 0.0; result[14] = 0.0; result[15] = 1.0; // четвертую строку не сохраняли
        return new Matrix3d(result);
      }
      return new Matrix3d();
    }

  }

}
