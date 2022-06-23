using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PI1_RoomFinishing
{
    /// <summary>
    /// Interaction logic for UserControlFloors.xaml
    /// </summary>
    public partial class UserControlFloors : Window
    {
        #region public members

        public List<FloorType> FloorTypesList { get; private set; }

        public List<Element> PlinthTypesList { get; private set; }

        public FloorType FloorTypeEl { get; private set; }

        public Element PlinthTypeEl { get; private set; }

        public double LevelOffset { get; private set; }

        #endregion

        #region private memders

        private UIDocument uidoc;

        private Document doc;

        #endregion

        #region constructor

        public UserControlFloors(UIDocument uidoc)
        {
            this.uidoc = uidoc;
            this.doc = uidoc.Application.ActiveUIDocument.Document;

            InitializeComponent();

            SetFloorTypesList();
            SetPlinthTypesList();

            DataContext = this;
        }

        #endregion

        #region events

        private void cmbFloorTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            FloorTypeEl = (FloorType)cmbFloorTypes.SelectedItem;
        }

        private void txtbLevelOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double number = Convert.ToDouble(txtbLevelOffset.Text);
                LevelOffset = UnitUtils.ConvertToInternalUnits(number, DisplayUnitType.DUT_MILLIMETERS);
            }
            catch { }
        }

        private void cmbPlinthTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PlinthTypeEl = (Element)cmbPlinthTypes.SelectedItem;
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void PreviewTextInputNumber(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9,-]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        #endregion

        #region private methods

        private void SetFloorTypesList()
        {
            FloorTypesList = new FilteredElementCollector(doc)
                .OfClass(typeof(FloorType))
                .Cast<FloorType>()
                .ToList();
        }

        private void SetPlinthTypesList()
        {
            PlinthTypesList = new FilteredElementCollector(doc)
                .OfCategory(BuiltInCategory.OST_GenericModel)
                .WhereElementIsElementType()
                .Where(plinth => plinth.Name.Contains("Плинтус"))
                .ToList();
        }

        #endregion
    }
}
