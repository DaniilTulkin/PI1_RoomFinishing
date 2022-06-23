using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Architecture;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using PI1_CORE;
using System.Collections.Generic;
using System.Linq;

namespace PI1_RoomFinishing
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]

    // Start command class.
    public class CommandFloors : IExternalCommand
    {
        #region public methods

        /// <summary>
        /// Overload this method to implement and external command within Revit.
        /// </summary>
        /// <param name="commandData">An ExternalCommandData object which contains reference to Application and View
        /// needed by external command.</param>
        /// <param name="message">Error message can be returned by external command. This will be displayed only if the command status
        /// was "Failed".  There is a limit of 1023 characters for this message; strings longer than this will be truncated.</param>
        /// <param name="elements">Element set indicating problem elements to display in the failure dialog.  This will be used
        /// only if the command status was "Failed".</param>
        /// <returns>
        /// The result indicates if the execution fails, succeeds, or was canceled by user. If it does not
        /// succeed, Revit will undo any changes made by the external command.
        /// </returns>
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            UIApplication uiapp = commandData.Application;
            UIDocument uidoc = uiapp.ActiveUIDocument;
            Document doc = uidoc.Document;

            ElementCategoryFilter floorFilter = new ElementCategoryFilter(BuiltInCategory.OST_Floors);
            XYZ vectorZ = XYZ.BasisZ.Negate();

            View3D view3D = new FilteredElementCollector(doc)
                .OfClass(typeof(View3D))
                .Cast<View3D>()
                .ToList()
                .First();

            var window = new UserControlFloors(uidoc);
            window.ShowDialog();

            FloorType floorType = window.FloorTypeEl;
            double leveleOffset = window.LevelOffset;

            IList<Reference> references = null;
            try
            {
                references = uidoc.Selection.PickObjects(ObjectType.Element, new SelectionFilter(), "Выберите помещения");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            using (TransactionGroup tGr = new TransactionGroup(doc, "Отделка пола"))
            {
                tGr.Start();
                if ((bool)window.chbFloor.IsChecked)
                {
                    using (Transaction t = new Transaction(doc, "Генерация отделки пола"))
                    {
                        t.Start();

                        foreach (Reference reference in references)
                        {
                            Room room = doc.GetElement(reference.ElementId) as Room;
                            ElementId levelId = room.LevelId;
                            Level level = doc.GetElement(levelId) as Level;
                            IList<BoundarySegment> boundarySegments = room.GetBoundarySegments(new SpatialElementBoundaryOptions())[0];

                            CurveArray curveArray = new CurveArray();
                            foreach (BoundarySegment boundarySegment in boundarySegments)
                            {
                                Curve boundaryCurve = boundarySegment.GetCurve();
                                curveArray.Append(boundaryCurve);
                            }

                            Floor floor = doc.Create.NewFloor(curveArray, floorType, level, false);
                            floor.get_Parameter(BuiltInParameter.FLOOR_HEIGHTABOVELEVEL_PARAM).Set(leveleOffset);
                        }

                        t.Commit();
                    }
                }

                if ((bool)window.chbPlinth.IsChecked)
                {
                    using (Transaction t = new Transaction(doc, "Генерация плинтуса"))
                    {
                        t.Start();

                        FamilySymbol plinthType = window.PlinthTypeEl as FamilySymbol;
                        plinthType.Activate();

                        foreach (Reference reference in references)
                        {
                            Room room = doc.GetElement(reference.ElementId) as Room;
                            IList<BoundarySegment> boundarySegments = room.GetBoundarySegments(new SpatialElementBoundaryOptions())[0];

                            LocationPoint locPoint = room.Location as LocationPoint;
                            XYZ point = locPoint.Point.Add(new XYZ(0, 0, 3));

                            ReferenceIntersector intersector = new ReferenceIntersector(floorFilter, FindReferenceTarget.Face, view3D);
                            Reference referenceFace = intersector.FindNearest(point, vectorZ).GetReference();

                            if (referenceFace == null)
                            {
                                continue;
                            }
                        
                            foreach (BoundarySegment boundarySegment in boundarySegments)
                            {
                                Line boundaryCurve = boundarySegment.GetCurve() as Line;

                                if (leveleOffset != 0)
                                {
                                    boundaryCurve = boundaryCurve.CreateTransformed(Transform.CreateTranslation(leveleOffset * XYZ.BasisZ)) as Line;
                                }

                                try
                                {
                                    doc.Create.NewFamilyInstance(referenceFace, boundaryCurve, plinthType);
                                }
                                catch (Autodesk.Revit.Exceptions.InvalidOperationException)
                                {
                                    TaskDialog.Show("Предупреждение", "Не было задано корректное смещение от уровня");
                                    return Result.Failed;
                                }
                            }
                        }

                        t.Commit();
                    }
                }

                tGr.Assimilate();
            }

            return Result.Succeeded;
        }

        /// <summary>
        /// Gets the path of the current command.
        /// </summary>
        /// <returns></returns>
        public static string GetPath()
        {
            return typeof(CommandFloors).Namespace + "." + nameof(CommandFloors);
        }

        #endregion
    }
}
