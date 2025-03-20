using Sopdu.helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Serialization;

namespace Sopdu.Devices.MotionControl.Base.UI
{
    /// <summary>
    /// Interaction logic for AxisPanel.xaml
    /// </summary>
    public partial class AxisPanel : UserControl
    {
        public AxisPanel()
        {
            InitializeComponent();
        }

        private void AlarmReset_Click(object sender, RoutedEventArgs e)
        {
            Axis axis = this.DataContext as Axis;
            try
            {
                axis.AlarmReset();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ExportList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //tmp data collect for axis informations.. to prepare for actual serialization
                PositionConfig recipe = new PositionConfig();
                recipe.PositionList = new System.Collections.Generic.List<AxisPosition>();
                Axis axis = this.DataContext as Axis;
                foreach (AxisPosition pos in axis.PositionList)
                {
                    recipe.PositionList.Add(pos);
                }
                //write xmlfile
                //// Create a new XmlSerializer instance with the type of the test class
                XmlSerializer SerializerObj = new XmlSerializer(typeof(PositionConfig));
                //// Create a new file stream to write the serialized object to a file
                TextWriter WriteFileStream = new StreamWriter(@"C:\" + axis.Name + ".xml");
                SerializerObj.Serialize(WriteFileStream, recipe);
                //// Cleanup
                WriteFileStream.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void HomeSearchStart_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Axis axis = this.DataContext as Axis;
            try
            {
                axis.StartHomeSearch(false);//emo set to off may need to get from seq or somewhere on emo status.
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void JogNegative_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Axis axis = this.DataContext as Axis;
                AxisPosition negativeJog = (AxisPosition)axis.PositionList[0].Clone();
                negativeJog.Coordinate = -(long.Parse(JogLength.Text));
                negativeJog.IsRelativePosition = true;
                axis.StartMove(negativeJog);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void JogPositive_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Axis axis = this.DataContext as Axis;
                AxisPosition positiveJog = (AxisPosition)axis.PositionList[0].Clone();
                positiveJog.Coordinate = (long.Parse(JogLength.Text));
                positiveJog.IsRelativePosition = true;
                axis.StartMove(positiveJog);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void Move_Click(object sender, RoutedEventArgs e)
        {
            Axis axis = this.DataContext as Axis;
            try
            {
                axis.StartMove(Int32.Parse(Position.Text));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void PositionList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                // ListBox item clicked - do some cool things here
                Position.Text = ((ListBox)sender).SelectedIndex.ToString();
                Axis axis = this.DataContext as Axis;
                Coordinate.Text = axis.PositionList[((ListBox)sender).SelectedIndex].Coordinate.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void RevertSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // ListBox item clicked - do some cool things here
                Position.Text = this.PositionList.SelectedIndex.ToString();
                Axis axis = this.DataContext as Axis;
                Coordinate.Text = axis.PositionList[this.PositionList.SelectedIndex].Coordinate.ToString();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void SaveSelected_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Axis axis = this.DataContext as Axis;
                this.Coordinate.Text = axis.CurrentCoordinate.ToString();

                axis.PositionList[int.Parse(Position.Text)].Coordinate = long.Parse(this.Coordinate.Text);//<=== should have been updated when saved?!
                GenericRecipe<PositionConfig> recipe = new GenericRecipe<PositionConfig>(axis.PositionFilePath);
                PositionConfig position = new PositionConfig();
                position.PositionList = new System.Collections.Generic.List<AxisPosition>();
                foreach (AxisPosition pos in axis.PositionList)
                {
                    position.PositionList.Add(pos);
                }
                recipe.Write(position);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ServoOff_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Axis axis = this.DataContext as Axis;
            try
            {
                axis.ServoOff();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void ServoOn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            Axis axis = this.DataContext as Axis;
            try
            {
                axis.ServoOn(false);//default false... emo? or should we use the module emo bool?
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
    }
}