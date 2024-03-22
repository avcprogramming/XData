// A>V>C> avc.programming@gmail.com https://sites.google.com/site/avcplugins/
// Отрывок кода чисто для примера для xData

#if BRICS
using Bricscad.ApplicationServices;
using Teigha.DatabaseServices;
using CadApp = Bricscad.ApplicationServices.Application;
#else
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using CadApp = Autodesk.AutoCAD.ApplicationServices.Application;
#endif

namespace AVC
{
  /// <summary>
  /// Расширения для работы с документом-чертежом
  /// Отрывок кода чисто для примера для xData
  /// </summary>
  internal static class 
  DocExt
  {
    /// <summary>
    /// Проверить наличие блокировки и если ее нет, то заблокировать. 
    /// Возвратит null или блокировку (в BricsCAD нельзя блокировать повторно)
    /// </summary>
    public static DocumentLock 
    LockIfNone(this Document doc, string nameForUndo)
    {
      if (doc == null || doc.Locked()) return null;
      return doc.LockDocument(DocumentLockMode.Write, nameForUndo, nameForUndo, true);
    }

    /// <summary>
    /// Проверить наличие блокировки
    /// </summary>
    public static bool 
    Locked(this Document doc)
    {
      if (doc == null) return false;
#if BRICS && !BRICS21 // в БриксКАД 17 просто нет LockMode - приходится использовать reflection
      MethodInfo lockMode = doc.GetType().GetMethod("LockMode", new Type[] { });
      DocumentLockMode mode = lockMode == null ? DocumentLockMode.None : (DocumentLockMode)lockMode.Invoke(doc, null);
#else
      DocumentLockMode mode = doc.LockMode(true);
#endif
      return mode != DocumentLockMode.None && mode != DocumentLockMode.NotLocked;
    }

    /// <summary>
    /// Активный документ заблокирован (можно редактировать объект Id) 
    /// или Id не из активного докумета и тогда можно записывать данные без блокировки 
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public static bool
    ReadyForWrite(this ObjectId id)
    {
      if (id.Database is null) return false;
      Document doc = CadApp.DocumentManager.MdiActiveDocument;
      if (doc is null || doc.Database != id.Database) return true; // можно редактировать объект так как он из другого чертежа
      return Locked(doc); // можно редактировать объект так как в документе есть блокировка
    }

  }
}
