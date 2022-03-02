using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace FloatingClock
{
    /// <summary>
    /// Interaction logic for AlarmDialog.xaml
    /// </summary>
    public partial class AlarmDialog : Window
    {
        public string Answer => txtAnswer.Text;
        public int SelectedTimeIndex => cmbTime.SelectedIndex;

        public AlarmDialog(string question, string defaultAnswer = "")
        {
            InitializeComponent();
            lblQuestion.Content = question;
            txtAnswer.Text = defaultAnswer;
        }

        private void Window_ContentRendered(object sender, EventArgs e)
        {
            txtAnswer.SelectAll();
            txtAnswer.Focus();
        }

        private void btnDialogOK_Click(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrWhiteSpace(Answer) && ((SelectedTimeIndex == 2 && Answer.Contains(':')) || (SelectedTimeIndex != 2 && ValidResult(Answer))))
            {
                this.DialogResult = true;
            }
            else
            {
                txtAnswer.BorderBrush = new SolidColorBrush(Colors.Red);
            }
        }

        private void txtAnswer_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            txtAnswer.BorderBrush = new SolidColorBrush(Colors.Black);
            if (!ValidResult(e.Text))
            {
                e.Handled = true;
            }
        }

        private bool ValidResult(string toTest)
        {
            var expression = SelectedTimeIndex == 2 ? "[^0-9,:]+" : "[^0-9]+";
            Regex regex = new Regex(expression);
            if (regex.IsMatch(toTest))
            {
                return false;
            }

            return true;
        }
    }
}
