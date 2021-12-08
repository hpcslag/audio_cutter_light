using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AudioPlayerSample
{
    // 定義框選事件參數 SelectionChangedEventArgs
    public class SelectionChangedEventArgs : EventArgs
    {
        public enum DirectionEnum { Min, Max };

        private readonly DirectionEnum direction;

        public SelectionChangedEventArgs(DirectionEnum direction)
        {
            this.direction = direction;
        }

        public DirectionEnum Direction
        {
            get { return this.direction; }
        }
    }

    // 定義一個框選事件
    public delegate void SelectionEventHandler(object sender, SelectionChangedEventArgs e);

    /// <summary>
    /// Very basic slider control with selection range.
    /// </summary>
    [Description("Very basic slider control with selection range.")]
    public partial class SelectionRangeSlider : UserControl
    {
        /// <summary>
        /// Minimum value of the slider.
        /// </summary>
        [Description("Minimum value of the slider.")]
        public float Min
        {
            get { return min; }
            set { min = value; Invalidate(); }
        }
        float min = 0;
        /// <summary>
        /// Maximum value of the slider.
        /// </summary>
        [Description("Maximum value of the slider.")]
        public float Max
        {
            get { return max; }
            set { max = value; Invalidate(); }
        }
        float max = 100;
        /// <summary>
        /// Minimum value of the selection range.
        /// </summary>
        [Description("Minimum value of the selection range.")]
        public float SelectedMin
        {
            get { return selectedMin; }
            set
            {
                selectedMin = value;
                if (SelectionChanged != null)
                    SelectionChanged(this, new SelectionChangedEventArgs(SelectionChangedEventArgs.DirectionEnum.Min));
                Invalidate();
            }
        }
        float selectedMin = 0;
        /// <summary>
        /// Maximum value of the selection range.
        /// </summary>
        [Description("Maximum value of the selection range.")]
        public float SelectedMax
        {
            get { return selectedMax; }
            set
            {
                selectedMax = value;
                if (SelectionChanged != null)
                    SelectionChanged(this, new SelectionChangedEventArgs(SelectionChangedEventArgs.DirectionEnum.Max));
                Invalidate();
            }
        }
        float selectedMax = 100;
        /// <summary>
        /// Current value.
        /// </summary>
        [Description("Current value.")]
        public float Value
        {
            get { return value; }
            set
            {
                this.value = value;
                if (ValueChanged != null)
                    ValueChanged(this, null);
                Invalidate();
            }
        }
        float value = 50;
        /// <summary>
        /// Fired when SelectedMin or SelectedMax changes.
        /// </summary>
        [Description("Fired when SelectedMin or SelectedMax changes.")]
        public event SelectionEventHandler SelectionChanged;
        /// <summary>
        /// Fired when Value changes.
        /// </summary>
        [Description("Fired when Value changes.")]
        public event EventHandler ValueChanged;

        public SelectionRangeSlider()
        {
            //avoid flickering
            SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            Paint += new PaintEventHandler(SelectionRangeSlider_Paint);
            MouseDown += new MouseEventHandler(SelectionRangeSlider_MouseDown);
            MouseMove += new MouseEventHandler(SelectionRangeSlider_MouseMove);
            Paint += new PaintEventHandler(Waveform_Paint);
        }

        public float[] storedWaveForm;
        void Waveform_Paint(object sender, PaintEventArgs e) {
            if (storedWaveForm != null)
            {
                Point[] points = new Point[storedWaveForm.Length];
                for (int i = 0; i < storedWaveForm.Length; i++)
                {
                    points[i] = new Point(i, (int)(storedWaveForm[i]*20));
                }

                float tension = 1.0F;
                e.Graphics.DrawCurve(Pens.Red, points, tension);
            }
        }

        void SelectionRangeSlider_Paint(object sender, PaintEventArgs e)
        {
            

            //paint background in white
            e.Graphics.FillRectangle(Brushes.White, ClientRectangle);
            //paint selection range in blue
            RectangleF selectionRect = new RectangleF(
                (selectedMin - Min) * Width / (Max - Min),
                0,
                (selectedMax - selectedMin) * Width / (Max - Min),
                Height);
            e.Graphics.FillRectangle(Brushes.LightGray, selectionRect);
            //draw a black frame around our control
            e.Graphics.DrawRectangle(Pens.Black, 0, 0, Width - 1, Height - 1);
            //draw a simple vertical line at the Value position
            e.Graphics.DrawLine(Pens.Green,
                (Value - Min) * Width / (Max - Min), 0,
                (Value - Min) * Width / (Max - Min), Height);
        }

        void SelectionRangeSlider_MouseDown(object sender, MouseEventArgs e)
        {
            //check where the user clicked so we can decide which thumb to move
            float pointedValue = Min + e.X * (Max - Min) / Width;
            float distValue = Math.Abs(pointedValue - Value);
            float distMin = Math.Abs(pointedValue - SelectedMin);
            float distMax = Math.Abs(pointedValue - SelectedMax);
            float minDist = Math.Min(distValue, Math.Min(distMin, distMax));
            if (minDist == distValue)
                movingMode = MovingMode.MovingValue;
            else if (minDist == distMin)
                movingMode = MovingMode.MovingMin;
            else
                movingMode = MovingMode.MovingMax;
            //call this to refreh the position of the selected thumb
            SelectionRangeSlider_MouseMove(sender, e);
        }

        void SelectionRangeSlider_MouseMove(object sender, MouseEventArgs e)
        {
            //if the left button is pushed, move the selected thumb
            if (e.Button != MouseButtons.Left)
                return;
            float pointedValue = Min + e.X * (Max - Min) / Width;
            if (movingMode == MovingMode.MovingValue)
                Value = pointedValue;
            else if (movingMode == MovingMode.MovingMin)
                SelectedMin = pointedValue;
            else if (movingMode == MovingMode.MovingMax)
                SelectedMax = pointedValue;
        }

        /// <summary>
        /// To know which thumb is moving
        /// </summary>
        enum MovingMode { MovingValue, MovingMin, MovingMax }
        MovingMode movingMode;
    }
}
