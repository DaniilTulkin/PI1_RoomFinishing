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
    /// Interaction logic for UserControlWalls.xaml
    /// </summary>
    public partial class UserControlWalls : Window
    {
        #region public members

        public List<WallType> WallTypesList { get; private set; }

        public WallType WallTypeEl { get; private set; }

        public double BottomOffset { get; private set; }

        public double TopOffset { get; private set; }

        #endregion

        #region private members

        private UIDocument uidoc;

        private Document doc;

        #endregion

        #region constructor

        /// <summary>
        /// Default constructor.
        /// Initializes a new instance of the <see cref="UserControlWalls"/> class.
        /// </summary>
        public UserControlWalls(UIDocument uidoc)
        {
            this.uidoc = uidoc;
            this.doc = uidoc.Application.ActiveUIDocument.Document;

            InitializeComponent();

            SetWallTypesList();
            DataContext = this;
        }

        #endregion

        #region events

        private void cmbWallTypes_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            WallTypeEl = (WallType)cmbWallTypes.SelectedItem;
        }

        private void txtbBottomOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double number = Convert.ToDouble(txtbBottomOffset.Text);
                BottomOffset = UnitUtils.ConvertToInternalUnits(number, DisplayUnitType.DUT_MILLIMETERS);
            }
            catch { }
        }

        private void txtbTopOffset_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                double number = Convert.ToDouble(txtbTopOffset.Text);
                TopOffset = UnitUtils.ConvertToInternalUnits(number, DisplayUnitType.DUT_MILLIMETERS);
            }
            catch { }
        }

        private void PreviewTextInputNumber(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9,-]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private void btnOK_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        #endregion

        #region private methods 

        private void SetWallTypesList()
        {
            WallTypesList = new FilteredElementCollector(doc)
                .OfClass(typeof(WallType))
                .Cast<WallType>()
                .Where(wallType => wallType.Width < 
                 UnitUtils.ConvertToInternalUnits(100, DisplayUnitType.DUT_MILLIMETERS))
                .ToList();
        }

        #endregion

    }
}
