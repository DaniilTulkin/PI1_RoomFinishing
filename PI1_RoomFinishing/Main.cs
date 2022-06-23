using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using PI1_CORE;
using PI1_RES;
using PI1_UI;
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace PI1_RoomFinishing
{
    [Transaction(TransactionMode.Manual)]
    [Regeneration(RegenerationOption.Manual)]

    // Running interface class
    public class Main : IExternalApplication
    {
        #region public methods

        /// <summary>
        /// Implement this method to execute some tasks when Autodesk Revit shuts down.
        /// </summary>
        /// <param name="application">A handle to the application being shut down.</param>
        /// <returns>
        /// Indicates if the external application completes its work successfully.
        /// </returns>
        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        /// <summary>
        /// Implement this method to create tab, ribbon and button or add elements if tab and ribbon was created when Autodesk Revit starts.
        /// </summary>
        /// <param name="application">A handle to the application being started.</param>
        /// <returns>
        /// Indicates if the external application completes its work successfully.
        /// </returns>
        public Result OnStartup(UIControlledApplication application)
        {
            string tabName = "PI1";
            string ribbonPanelName = RibbonName.Name(RibbonNameType.AR); ;
            RibbonPanel ribbonPanel = null;

            try
            {
                application.CreateRibbonTab(tabName);
            }
            catch { }

            try
            {
                ribbonPanel = application.CreateRibbonPanel(tabName, ribbonPanelName);
            }
            catch
            {
                ribbonPanel = application.GetRibbonPanels(tabName)
                    .FirstOrDefault(panel => panel.Name.Equals(ribbonPanelName));
            }

            var btnWallsData = new RevitPushButtonData
            {
                Label = "Отделка стен",
                Panel = ribbonPanel,
                ToolTip = "Генерация отделки стен",
                CommandNamespacePath = CommandWalls.GetPath(),
                ImageName = "icon_PI1_RoomFinishing_Walls_16x16.png",
                LargeImageName = "icon_PI1_RoomFinishing_Walls_32x32.png"
            };

            var btnFloorsData = new RevitPushButtonData
            {
                Label = "Отделка пола",
                Panel = ribbonPanel,
                ToolTip = "Генерация отделки пола с плинтусом",
                CommandNamespacePath = CommandFloors.GetPath(),
                ImageName = "icon_PI1_RoomFinishing_Floors_16x16.png",
                LargeImageName = "icon_PI1_RoomFinishing_Floors_32x32.png"
            };

            //var btnCeilingsData = new RevitPushButtonData
            //{
            //    Label = "Отделка потолка",
            //    Panel = ribbonPanel,
            //    ToolTip = "Генерация отделки потолка",
            //    CommandNamespacePath = CommandCeilings.GetPath(),
            //    ImageName = "icon_Test_16x16.png",
            //    LargeImageName = "icon_Test_32x32.png"
            //};

            PulldownButtonData groupButtonData = new PulldownButtonData("Finishing", "Отделка")
            {
                ToolTip = "Инструменты генерации отделки помещения",
                Image = ResourceImage.GetIcon("icon_PI1_RoomFinishing_Walls_16x16.png"),
                LargeImage = ResourceImage.GetIcon("icon_PI1_RoomFinishing_Walls_32x32.png")
            };

            PulldownButton pulldownButton = ribbonPanel.AddItem(groupButtonData) as PulldownButton;
            var btnWalls = CreateRibbonItem(btnWallsData, pulldownButton);
            var btnFloors = CreateRibbonItem(btnFloorsData, pulldownButton);
            //var btnCielings = CreateRibbonItem(btnCeilingsData, pulldownButton);

            return Result.Succeeded;
        }

        #endregion

        #region prevate methods

        private static RibbonItem CreateRibbonItem(RevitPushButtonData data, PulldownButton pulldownButton)
        {
            // The button name based on unique identifier.
            var btnDataName = Guid.NewGuid().ToString();

            // Get assembly location.
            StackTrace stackTrace = new StackTrace();
            var assembly = stackTrace.GetFrame(0).GetMethod().DeclaringType.Assembly;
            string aLoc = assembly.Location;

            // Sets the button data.
            var btnData = new PushButtonData(btnDataName, data.Label, aLoc, data.CommandNamespacePath)
            {
                ToolTip = data.ToolTip,
                Image = ResourceImage.GetIcon(data.ImageName),
                LargeImage = ResourceImage.GetIcon(data.LargeImageName),
                //ToolTipImage = ResourceImage.GetIcon(data.ToolTipImageName)
            };

            // Return created button and host it on panel
            return pulldownButton.AddPushButton(btnData);
        }

        #endregion
    }
}
