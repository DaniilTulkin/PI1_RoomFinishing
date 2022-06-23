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
    public class CommandWalls : IExternalCommand
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

            var window = new UserControlWalls(uidoc);
            window.ShowDialog();

            ElementId wallTypeId = window.WallTypeEl.Id;
            double curvesOffset = window.WallTypeEl.Width / 2;
            double bottomOffset = window.BottomOffset;
            double topOffset = window.TopOffset;

            var levelIds = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .ToElementIds()
                .ToList();
            levelIds.OrderBy(levelId => ((Level)doc.GetElement(levelId)).Elevation);

            IList<Reference> references = null;
            try
            {
                references = uidoc.Selection.PickObjects(ObjectType.Element, new SelectionFilter(), "Выберите помещения");
            }
            catch (Autodesk.Revit.Exceptions.OperationCanceledException)
            {
                return Result.Cancelled;
            }

            Dictionary<Element, Wall> wallsToJoinDict = new Dictionary<Element, Wall>();

            using (TransactionGroup tGr = new TransactionGroup(doc, "Отделка стен"))
            {
                tGr.Start();

                using (Transaction t = new Transaction(doc, "Генерация отделки стен"))
                {
                    t.Start();

                    foreach (Reference reference in references)
                    {
                        Room room = doc.GetElement(reference.ElementId) as Room;

                        ElementId bottomlevelId = room.LevelId;

                        ElementId upperLevelId = null;
                        foreach (ElementId levelId in levelIds)
                        {
                            int index = levelIds.IndexOf(bottomlevelId);
                            upperLevelId = levelIds[index + 1];
                        }

                        IList<BoundarySegment> boundarySegments = room.GetBoundarySegments(new SpatialElementBoundaryOptions())[0];

                        CurveLoop curveLoop = new CurveLoop();
                        foreach (BoundarySegment boundarySegment in boundarySegments)
                        {
                            Curve boundaryCurve = boundarySegment.GetCurve();
                            Curve curve = Geometry.CurveOffset(boundaryCurve, curvesOffset);

                            Wall wall = Wall.Create(doc, curve, wallTypeId, bottomlevelId, 10, bottomOffset, true, false);
                            wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(upperLevelId);
                            wall.get_Parameter(BuiltInParameter.WALL_TOP_OFFSET).Set(topOffset);

                            Element element = doc.GetElement(boundarySegment.ElementId);
                            int categoryInt = element.Category.Id.IntegerValue;
                            int biCategoryInt = (int)BuiltInCategory.OST_Walls;
                            if (categoryInt == biCategoryInt)
                            {
                                wallsToJoinDict[element] = wall;
                            }
                        }
                    }

                    t.Commit();
                }

                using (Transaction t = new Transaction(doc, "Соединение стен"))
                {
                    t.Start();

                    foreach (KeyValuePair<Element, Wall> wall in wallsToJoinDict)
                    {
                        JoinGeometryUtils.JoinGeometry(doc, wall.Key, wall.Value);
                    }

                    t.Commit();
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
            return typeof(CommandWalls).Namespace + "." + nameof(CommandWalls);
        }

        #endregion
    }
}
