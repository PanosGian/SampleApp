
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Diagnostics;
using System.Windows.Forms;
using System.Linq;
using System.Xml.Linq;
using System.Threading.Tasks;

using System.Configuration;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using static System.Math;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using static System.Windows.Forms.DataFormats;

//SampleApp Class
namespace SampleApp
{
	//Thi structure is used to define and select X, Y axes plot options
	public struct PlotAxisOptions
	{
		public string Title;
		public LinearLogSacle LinearLog; //Enum
		public bool DrawLabels;
		public bool DrawTitle;
		public Font TitleFont;
		public Font LabelsFont;
		public Side PlotSide; //Enum
		public AxisOrientation Orientation; //Enum
		public RelPosition TickRelPos; //Enum
		public RelPosition TickLabelRelPos; //Enum
		public string LabelsFormat;
		public Pen PenMajorLine;
		public Pen PenMinorLine;
		public int MajorTickSize;
		public int MinorTickSize;
		public Brush ABrush;
	}

	public enum LinearLogSacle
	{
		Linear = 0,
		Log = 1
	}

	public enum Side
	{
		Left = 0,
		Top = 1,
		Right = 2,
		Bottom = 3
	}

	public enum AxisOrientation
	{
		Horizontal = 0,
		Vertical = 1,
		HorizontalAquiferSetup = 2
	}

	public enum RelPosition
	{
		Inside = 0,
		Outside = 1
	}



	public partial class Form1
	{
		//Some global variables
		public Form1()
		{
			InitializeComponent();
		}

		private DataTable TblData;
		private List<string> LCurveIDs = new List<string>();
		private string[] CurveIDs;
		private Graphics G;
		public SolidBrush sbrush1 = new SolidBrush(Color.Black);
		public SolidBrush ABrush = new SolidBrush(Color.Black);
		public Pen Pen1 = new Pen(Color.Black);
		public Pen penLine = new Pen(Color.Black);
		private SizeF s_wh = new SizeF();
		private SizeF s_wh1 = new SizeF();
		private SizeF s_wh2 = new SizeF();
		private SizeF s_wh3 = new SizeF();
		private Bitmap bmp; //plot area bitmap
		private bool plotReady;

		private void Form1_Load(object sender, EventArgs e)
		{
			plotReady = false;

		}

		//Event Handler - Handles OpenFile dialog from the Menu bar
		private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
		{
			OpenFileTxt();
		}

		public void OpenFileTxt()
		{
			OpenFileDialog1.Filter = "Text Tab delimited (*.TXT)|*.TXT"; // "Text Space delimited (*.txt)|*.txt"
			//1.  READ IN THE DATA FILE 

			DataRow dr = null;
			string[] TableHeadings = null;
			string fileName = null;
			string dirName = null;
			string TextLine = null;
			string fileNameWitoutPath = null;
			string delimiter = null;
			int f_in = 0;
			int i = 0;
			int j = 0;
			delimiter = " "; // or "\t"

			//Select and import a file
			if (OpenFileDialog1.ShowDialog() == System.Windows.Forms.DialogResult.OK)
			{
				// Try
				fileName = OpenFileDialog1.FileName;
				fileNameWitoutPath = Path.GetFileName(fileName);
				dirName = Path.GetDirectoryName(fileName);
				f_in = Microsoft.VisualBasic.FileSystem.FreeFile(); //'EXAMPLE.DAT'
				TblData = new DataTable();

				Microsoft.VisualBasic.FileSystem.FileOpen(f_in, fileName, Microsoft.VisualBasic.OpenMode.Input);

				//Get and define the DataTable Headings
				TextLine = Convert.ToString(clean_string(Microsoft.VisualBasic.FileSystem.LineInput(f_in))); //Cleans whites paces
				TableHeadings = Microsoft.VisualBasic.Strings.Split(TextLine, delimiter);

				//Create a datatable object to fetch and populate with the input data 
				for (i = 0; i < TableHeadings.Length; i++)
				{
					switch (TableHeadings[i].ToUpper())
					{
						case "ID":
							TblData.Columns.Add(TableHeadings[i].ToUpper()).DataType = typeof(System.Int32);
							break;
						case "X":
						case "Y":
							TblData.Columns.Add(TableHeadings[i].ToUpper()).DataType = typeof(System.Double);
							break;
						default:
							TblData.Columns.Add(TableHeadings[i].ToUpper()).DataType = typeof(System.String);
							break;
					}

				}
				object[] rowValues = null;
				LCurveIDs.Clear();

				while (!Microsoft.VisualBasic.FileSystem.EOF(f_in)) // parser.EndOfData
				{
					TextLine = Convert.ToString(clean_string(Microsoft.VisualBasic.FileSystem.LineInput(f_in)));
					rowValues = TextLine.Split(' ');
					dr = TblData.NewRow();
					for (i = 0; i < TableHeadings.Length; i++)
					{
						dr[i] = rowValues[i];
					}
					LCurveIDs.Add(Convert.ToString(dr["CURVE"]));
					TblData.Rows.Add(dr);
				}

				CurveIDs = LCurveIDs.Distinct().ToArray();
				
				// DataTableSort(TblData, "X ASC")
				
				//Populate DataGridView with inputa data
				DataGridView1.DataSource = TblData;

				//Populate combobox1 with the curve IDs
				if (CurveIDs.Length > 0)
				{
					for (j = 0; j < CurveIDs.Length; j++)
					{
						ComboBox1.Items.Add(CurveIDs[j]);
					}
					ComboBox1.SelectedIndex = 0;
					plotReady = true;
				}


				ToolStripStatusLabel1.Text = "Input data imported successfully. #Rows = " + TblData.Rows.Count + " # CurveIDs = " + CurveIDs.Length;

				if (plotReady)
				{
                    PlotData();
				}
				// Application.DoEvents()
			}
		}



  //Set up plot area
		public void prepare_screen_plot()
		{
			//Reset plot polygons
			bmp = new Bitmap(PictureBox1.Width, PictureBox1.Height);
			PictureBox1.BackgroundImage = bmp;
			G = Graphics.FromImage(bmp);
			G.Clear(PictureBox1.BackColor);
			G.SmoothingMode = SmoothingMode.AntiAlias;
		}


		public void redraw(object sender, EventArgs e)
		{
			PlotData();
        }


		//Plot the selected data
		public void PlotData()
		{
            if (plotReady)
            {

                int plotWidth = 0;
                int plotHeight = 0;
                float majorTickSpacingX = 0F;
                float minorTickSpacingX = 0F;
                float majorTickSpacingY = 0F;
                float minorTickSpacingY = 0F;

                Font txtf1 = new Font("Arial", 11, FontStyle.Regular);
                Font txtf2 = new Font("Arial", 10, FontStyle.Regular);

                prepare_screen_plot();

                //Graph coordinates
                float plotBufferX = 0F;
                float plotBufferY = 0F;
                plotBufferX = (float)(0.1 * PictureBox1.Width);
                plotBufferY = (float)(0.1 * PictureBox1.Height);
                plotWidth = Convert.ToInt32(PictureBox1.Width - 2 * plotBufferX);
                plotHeight = Convert.ToInt32(PictureBox1.Height - 2 * plotBufferX);

                PointF BL = new PointF(); //plot corners: BL=BottomLeft, BottomRight, TopLeft, TopRight
                PointF BR = new PointF();
                PointF TL = new PointF();
                PointF TR = new PointF();

                BL.X = plotBufferX;
                BL.Y = plotBufferY + plotHeight;

                BR.X = plotBufferX + plotWidth;
                BR.Y = BL.Y;

                TL.X = BL.X;
                TL.Y = plotBufferY;

                TR.X = BR.X;
                TR.Y = TL.Y;


                G.DrawLine(Pen1, BL, BR);
                G.DrawLine(Pen1, BL, TL);
                G.DrawLine(Pen1, TL, TR);
                G.DrawLine(Pen1, BR, TR);

                //SET PLOT AXIS OPRIONS 
                PlotAxisOptions axisX = new PlotAxisOptions(); //axis plot options
                PlotAxisOptions axisY = new PlotAxisOptions();
                //Axis X plot options
                axisX.LinearLog = LinearLogSacle.Linear;
                axisX.Title = "X";
                axisX.PlotSide = Side.Bottom;
                axisX.Orientation = AxisOrientation.Horizontal;
                axisX.DrawLabels = true;
                axisX.DrawTitle = true;
                axisX.TickRelPos = RelPosition.Inside;
                axisX.TickLabelRelPos = RelPosition.Outside;
                axisX.LabelsFormat = "F1";
                axisX.TitleFont = txtf1;
                axisX.LabelsFont = txtf2;

                //Axis Y plot options

                axisY.LinearLog = LinearLogSacle.Linear;
                axisY.Title = "Y";
                axisY.PlotSide = Side.Left;
                axisY.Orientation = AxisOrientation.Vertical;
                axisY.DrawLabels = true;
                axisY.DrawTitle = true;
                axisY.TickRelPos = RelPosition.Inside;
                axisY.TickLabelRelPos = RelPosition.Outside;
                axisY.LabelsFormat = "F1";
                axisY.TitleFont = txtf1;
                axisY.LabelsFont = txtf2;



                DataRow[] drs = null;
                string Expression = null;
                List<double> LX = new List<double>();
                List<double> LY = new List<double>();
                Expression = "CURVE ='" + ComboBox1.SelectedItem.ToString() + "'";
                drs = TblData.Select(Expression, string.Empty);

                for (var j = 0; j < drs.Length; j++)
                {
                    LX.Add((double)drs[j]["X"]);
                    LY.Add((double)drs[j]["Y"]);
                }



                double xMin = 0.0;
                double xMax = 0.0;
                double yMin = 0.0;
                double yMax = 0.0;
                xMin = LX.Min(); //TblData.Compute("min(X)", String.Empty)
                xMax = LX.Max(); //TblData.Compute("max(X)", String.Empty)
                yMin = LY.Min(); //TblData.Compute("min(Y)", String.Empty)
                yMax = LY.Max(); //TblData.Compute("max(Y)", String.Empty)


                majorTickSpacingX = (float)(xMax - xMin) / 10;
                majorTickSpacingY = (float)(yMax - yMin) / 10;
                minorTickSpacingX = majorTickSpacingX / 2;
                minorTickSpacingY = majorTickSpacingY / 2;

                PlotXAxis(G, (float)xMin, (float)xMax, BL, BR, majorTickSpacingX, minorTickSpacingX, axisX);
                PlotXAxis(G, (float)yMin, (float)yMax, BL, TL, majorTickSpacingY, minorTickSpacingY, axisY);

                //PlotData
                PointF[] pt = new PointF[drs.Length]; //plot data points
                for (var i = 0; i < drs.Length; i++)
                {
                    pt[i].X = xmap((double)drs[i]["X"], xMin, xMax, Convert.ToInt32(BL.X), Convert.ToInt32(BR.X));
                    pt[i].Y = xmap((double)drs[i]["Y"], yMin, yMax, Convert.ToInt32(BL.Y), Convert.ToInt32(TL.Y));
                }

                GraphicsPath pathLine = new GraphicsPath();
                pathLine.AddCurve(pt, 0);
                G.DrawPath(Pen1, pathLine);
            }
        }


		//THIS SECTION CREATES VARIOUS FUNCTIONS USED TO PLOT DATA. e.g PlotAxis, CleanString, XMap
		
		// Cleans whitespace spaces afrom string
        public object clean_string(string str)
        {
            str = str.Replace("\t", " "); // convert tabs to spaces first
            while ((str.IndexOf("  ") + 1) != 0) // Find and replace any occurences of multiple spaces
            {
                str = str.Replace("  ", " "); // if true, the string still contains double spaces, replace with single space
            }
            return (str.Trim(' ')); // Remove any leading or training spaces and return
        }


		//Map plot data on screen coordinates
        public float xmap(double r, double rmin, double rmax, int smin, int smax)
		{
			float s = 0F;
			try
			{
				if (Math.Abs(rmax - rmin) > 0)
				{
					s = (float)(smin + (smax - smin) * (r - rmin) / (rmax - rmin));
					return (s);
				}
				else
				{
					return (float)(rmin);
				}
			}
			catch (Exception ex)
			{
				// MsgBox(ex.Message & ". Error in X mapping Function")
			}

			return 0F;
		}


		//This function plos X or Y axis based on additional parameters specified in the 'PlotAxisOption structure 
	 public void PlotXAxis(Graphics Gp, float ValueMin, float ValueMax, PointF pmin, PointF pmax, float MajorTSpacing, float MinorTSpacing, PlotAxisOptions AxisOptions)
		{
			int MajorTSize = AxisOptions.MajorTickSize;
			int MinorTSize = AxisOptions.MinorTickSize;

			float NMajorTSteps = 0F;
			float NMinorTSteps = 0F;
			float TickValue = 0F;
			float MinorTValue = 0F;

			if (AxisOptions.PenMajorLine == null)
			{
				AxisOptions.PenMajorLine = penLine;
				AxisOptions.PenMinorLine = penLine;
				AxisOptions.ABrush = ABrush;
			}
			//AxisOptions.

			PointF pta = new PointF();
			string LabelFormatLog = null;
			List<float> LLabelW = new List<float>();
			string Stext = null;
			s_wh = Gp.MeasureString(ValueMax.ToString(AxisOptions.LabelsFormat), AxisOptions.LabelsFont);
			float FontWidth = 0F;
			float FontHeight = 0F;
			float dx = 0F;
			float dy = 0F;
			FontWidth = s_wh.Width;
			FontHeight = s_wh.Height;

			//OPTIMISE / CHECK MAJOR TICK SPACING-------------------------------------------------------------------------------------------------------------
			float TW = 0F; //Total Axis Width in pizels
			float TH = 0F;
			float SW = 0F; // Label Width and Height, SpacingWidth (SW), TotalSingleLabelWidth(TSLW), TotalLabelWidth(TLW)
			float SH = 0F;
			float TSLW = 0F;
			float TSLH = 0F;
			float MinMajorTickSpacingPixel = 0F;
			float MinMajorTickSpacingData = 0F;
			int MaxNL = 0; //Maximum Number of Labels
			float SpaceSizeFactor = 0.5F;

			List<string> LAxisLables = new List<string>();
			float dxx = 0F;
			float dyy = 0F;
			s_wh1 = Gp.MeasureString(ValueMax.ToString(), AxisOptions.LabelsFont);


			dxx = 0;
			dyy = (float)(0.2 * s_wh1.Height);




			switch (AxisOptions.Orientation)
			{

				case AxisOrientation.Horizontal:
				case AxisOrientation.HorizontalAquiferSetup:

					TW = Abs(pmax.X - pmin.X);
					SW = SpaceSizeFactor * s_wh1.Width;
					TSLW = s_wh1.Width + SW;
					MaxNL = (int)Ceiling((TW + TSLW) / TSLW);
					MinMajorTickSpacingPixel = TW / (MaxNL - 1);
					MinMajorTickSpacingData = Abs(ValueMax - ValueMin) * (MinMajorTickSpacingPixel / TW);
					break;

				case AxisOrientation.Vertical:

					TH = Abs(pmax.Y - pmin.Y);
					SH = SpaceSizeFactor * s_wh1.Height;
					TSLH = s_wh1.Height + SH;
					MaxNL = (int)Ceiling((TH + TSLH) / TSLH);
					MinMajorTickSpacingPixel = TH / (MaxNL - 1);
					MinMajorTickSpacingData = Abs(ValueMax - ValueMin) * (MinMajorTickSpacingPixel / TH);
					break;

			}

			if (MajorTSpacing < MinMajorTickSpacingData)
			{
				MajorTSpacing = MinMajorTickSpacingData;
				MinorTSpacing = MajorTSpacing / 4;
			}
			//-----------------------------------------------------------------------------------------------------------------------------------------------




			switch (AxisOptions.LinearLog)
			{
				case LinearLogSacle.Linear:
					NMajorTSteps = (ValueMax - ValueMin) / MajorTSpacing;
					NMinorTSteps = MajorTSpacing / MinorTSpacing;
					break;
				case LinearLogSacle.Log:
					NMajorTSteps = (float)(Log10(ValueMax) - Log10(ValueMin)); // + 1
					NMinorTSteps = 9;
					break;
			}

			//Draw the plot axes
			Gp.DrawLine(AxisOptions.PenMajorLine, pmin, pmax);
			if (float.IsNaN(NMajorTSteps) == false)
			{

				for (var i = 0; i <= NMajorTSteps; i++)
				{
					//DRAW MAJOR TICKS
					switch (AxisOptions.Orientation)
					{
						case AxisOrientation.Horizontal:
						case AxisOrientation.HorizontalAquiferSetup:
							//Major Ticks
							switch (AxisOptions.LinearLog)
							{
								case LinearLogSacle.Linear:
									TickValue = ValueMin + i * MajorTSpacing;
									pta.X = xmap(TickValue, ValueMin, ValueMax, Convert.ToInt32(pmin.X), Convert.ToInt32(pmax.X));
									break;
								case LinearLogSacle.Log:
									TickValue = (float)(ValueMin * Pow(10, i));
									pta.X = xmap(Log10(TickValue), Log10(ValueMin), Log10(ValueMax), Convert.ToInt32(pmin.X), Convert.ToInt32(pmax.X));

									switch (AxisOptions.Orientation)
									{
										case AxisOrientation.HorizontalAquiferSetup:
											if (TickValue <= ValueMax)
											{
												pta.X = xmap(Log10(TickValue), Log10(ValueMin), Log10(ValueMax), Convert.ToInt32(pmin.X), Convert.ToInt32(pmax.X));
											}
											else
											{
												TickValue = ValueMax;
												pta.X = xmap(Log10(TickValue), Log10(ValueMin), Log10(ValueMax), Convert.ToInt32(pmin.X), Convert.ToInt32(pmax.X));
											}
											break;
										default:
											pta.X = xmap(Log10(TickValue), Log10(ValueMin), Log10(ValueMax), Convert.ToInt32(pmin.X), Convert.ToInt32(pmax.X));
											break;
									}


									if (Log10(TickValue) >= 0)
									{
											LabelFormatLog = "F0";
									}

									else
									{
											LabelFormatLog = "F" + Math.Ceiling(Abs(Log10(TickValue))); //- 1
									}
									AxisOptions.LabelsFormat = LabelFormatLog;
									break;
							}
							pta.Y = pmin.Y;
							s_wh = Gp.MeasureString(TickValue.ToString(AxisOptions.LabelsFormat), AxisOptions.LabelsFont);
							switch (AxisOptions.PlotSide)
							{
								case Side.Top:

									if (pta.X <= Ceiling(pmax.X))
									{
										Gp.DrawLine(AxisOptions.PenMajorLine, pta.X, pta.Y, pta.X, pta.Y + MajorTSize);
									}

									if (AxisOptions.DrawLabels == true)
									{
										if (pta.X <= Ceiling(pmax.X))
										{
											LAxisLables.Add(TickValue.ToString(AxisOptions.LabelsFormat));
											Gp.DrawString(TickValue.ToString(AxisOptions.LabelsFormat), AxisOptions.LabelsFont, AxisOptions.ABrush, (float)(pta.X - 0.5 * s_wh.Width), pta.Y - s_wh.Height - dyy);
										}
									}
									break;

								case Side.Bottom:
									if (pta.X <= pmax.X)
									{
										Gp.DrawLine(AxisOptions.PenMajorLine, pta.X, pta.Y, pta.X, pta.Y - MajorTSize);
									}
									if (AxisOptions.DrawLabels == true)
									{
										switch (AxisOptions.Orientation)
										{
											case AxisOrientation.HorizontalAquiferSetup:
												if (TickValue <= ValueMax)
												{
													if (AxisOptions.LinearLog == LinearLogSacle.Linear)
													{
														AxisOptions.LabelsFormat = "F0";
													}

													LAxisLables.Add(TickValue.ToString(AxisOptions.LabelsFormat));
														Gp.DrawString(TickValue.ToString(AxisOptions.LabelsFormat), AxisOptions.LabelsFont, AxisOptions.ABrush, (float)(pta.X - 0.5 * s_wh.Width), pta.Y - s_wh.Height - MajorTSize);

												}
												break;
											default:
												if (pta.X <= pmax.X)
												{
													LAxisLables.Add(TickValue.ToString(AxisOptions.LabelsFormat));
													Gp.DrawString(TickValue.ToString(AxisOptions.LabelsFormat), AxisOptions.LabelsFont, AxisOptions.ABrush,(float) (pta.X - 0.5 * s_wh.Width), pta.Y + dyy);
												}
												break;
										}
									}
									break;
							}



							//Minor Ticks
							for (var j = 1; j <= NMinorTSteps; j++)
							{
								switch (AxisOptions.LinearLog)
								{
									case LinearLogSacle.Linear:
										MinorTValue = TickValue + j * MinorTSpacing;
										pta.X = xmap(MinorTValue, 0, ValueMax, Convert.ToInt32(pmin.X), Convert.ToInt32(pmax.X));
										break;
									case LinearLogSacle.Log:
										MinorTValue = TickValue + j * TickValue;
										pta.X = xmap(Log10(MinorTValue), Log10(ValueMin), Log10(ValueMax), Convert.ToInt32(pmin.X), Convert.ToInt32(pmax.X));
										break;
								}
								pta.Y = pmin.Y;
								if (MinorTValue < ValueMax)
								{

									switch (AxisOptions.PlotSide)
									{
										case Side.Top:
											Gp.DrawLine(AxisOptions.PenMajorLine, pta.X, pta.Y, pta.X, pta.Y + MinorTSize);
											break;
										case Side.Bottom:
											Gp.DrawLine(AxisOptions.PenMajorLine, pta.X, pta.Y, pta.X, pta.Y - MinorTSize);
											break;
									}



								}
							}
							break;
						case AxisOrientation.Vertical:
							//Major Ticks
							switch (AxisOptions.LinearLog)
							{
								case LinearLogSacle.Linear:
									TickValue = ValueMin + i * MajorTSpacing;
									pta.Y = xmap(TickValue, ValueMin, ValueMax, Convert.ToInt32(pmin.Y), Convert.ToInt32(pmax.Y));
									break;
								case LinearLogSacle.Log:
									TickValue = (float)(ValueMin * Pow(10, i));
									pta.Y = xmap(Log10(TickValue), Log10(ValueMin), Log10(ValueMax), Convert.ToInt32(pmin.Y), Convert.ToInt32(pmax.Y));



									if (Log10(TickValue) >= 0)
									{
											LabelFormatLog = "F0";
									}

									else
									{
											LabelFormatLog = "F" + Math.Ceiling(Abs(Log10(TickValue))); //- 1
									}
									AxisOptions.LabelsFormat = LabelFormatLog;
									break;
							}

							pta.X = pmin.X;
							switch (AxisOptions.PlotSide)
							{
								case Side.Left:

									if (pmax.Y < pmin.Y)
									{
										if (pta.Y >= pmax.Y)
										{
											Gp.DrawLine(AxisOptions.PenMajorLine, pta.X, pta.Y, pta.X + MajorTSize, pta.Y);
										}
									}
									else
									{
										if (pta.Y <= pmax.Y)
										{
											Gp.DrawLine(AxisOptions.PenMajorLine, pta.X, pta.Y, pta.X + MajorTSize, pta.Y);
										}
									}


									s_wh = Gp.MeasureString(TickValue.ToString(AxisOptions.LabelsFormat), AxisOptions.LabelsFont);
									//Case LEFT axis...
									if (AxisOptions.DrawLabels == true)
									{
										Stext = TickValue.ToString(AxisOptions.LabelsFormat);
										s_wh1 = Gp.MeasureString(Stext, AxisOptions.LabelsFont);
										LLabelW.Add(s_wh1.Width);
										Gp.DrawString(Stext, AxisOptions.LabelsFont, AxisOptions.ABrush, pta.X - s_wh.Width, (float)(pta.Y - 0.5 * s_wh.Height));
									}
									break;

								case Side.Right:

									if (pmax.Y < pmin.Y)
									{
										if (pta.Y >= pmax.Y)
										{
											Gp.DrawLine(AxisOptions.PenMajorLine, pta.X, pta.Y, pta.X - MajorTSize, pta.Y);
										}
									}
									else
									{
										if (pta.Y <= pmax.Y)
										{
											Gp.DrawLine(AxisOptions.PenMajorLine, pta.X, pta.Y, pta.X - MajorTSize, pta.Y);
										}
									}



									if (AxisOptions.DrawLabels == true)
									{
										Stext = TickValue.ToString(AxisOptions.LabelsFormat);
										s_wh1 = Gp.MeasureString(Stext, AxisOptions.LabelsFont);
										LLabelW.Add(s_wh1.Width);
										Gp.DrawString(Stext, AxisOptions.LabelsFont, AxisOptions.ABrush, pta.X, (float)(pta.Y - 0.5 * s_wh.Height));
									}
									break;

							}

							//Minor Ticks
							for (var j = 1; j <= NMinorTSteps; j++)
							{

								switch (AxisOptions.LinearLog)
								{
									case LinearLogSacle.Linear:
										MinorTValue = TickValue + j * MinorTSpacing;
										pta.Y = xmap(MinorTValue, ValueMin, ValueMax, Convert.ToInt32(pmin.Y), Convert.ToInt32(pmax.Y));
										break;
									case LinearLogSacle.Log:
										MinorTValue = TickValue + j * TickValue;
										pta.Y = xmap(Log10(MinorTValue), Log10(ValueMin), Log10(ValueMax), Convert.ToInt32(pmin.Y), Convert.ToInt32(pmax.Y));
										break;
								}
								if (MinorTValue < ValueMax)
								{

									switch (AxisOptions.PlotSide)
									{
										case Side.Left:
											Gp.DrawLine(AxisOptions.PenMajorLine, pta.X, pta.Y, pta.X + MinorTSize, pta.Y);
											break;
										case Side.Right:
											Gp.DrawLine(AxisOptions.PenMajorLine, pta.X, pta.Y, pta.X - MinorTSize, pta.Y);
											break;
									}

								}
							}
							break;
					}
				}
			}
			//=============================

			//Get Maximum Axis Label string size for plotting acurately the Axis title, etc

			float AxisLabelMaxWidth = 0F;
			float AxisLabelMaxHeight = 0F;
			AxisLabelMaxWidth = 0;
			AxisLabelMaxHeight = 0;
			for (int ii = 0; ii < LAxisLables.Count; ii++)
			{
				s_wh1 = Gp.MeasureString(LAxisLables[ii], AxisOptions.LabelsFont);
				AxisLabelMaxWidth = Math.Max(AxisLabelMaxWidth, s_wh1.Width);
				AxisLabelMaxHeight = Math.Max(AxisLabelMaxHeight, s_wh1.Height);
			}


			PointF ptEqn = new PointF();
			//PLOT AXIS TITLE
			if (AxisOptions.DrawTitle == true)
			{
				switch (AxisOptions.Orientation)
				{
					case AxisOrientation.Horizontal:
						s_wh = Gp.MeasureString(AxisOptions.Title, AxisOptions.TitleFont);
						dy = s_wh.Height;
						switch (AxisOptions.PlotSide)
						{
							case Side.Top:
								ptEqn.X = (float)(pmin.X + 0.5 * (pmax.X - pmin.X - s_wh.Width));
								ptEqn.Y = (float)(pmin.Y - (1.6 * s_wh.Height) - dyy);

								Gp.DrawString(AxisOptions.Title, AxisOptions.TitleFont, AxisOptions.ABrush, (float)(pmin.X + 0.5 * (pmax.X - pmin.X - s_wh.Width)), pmin.Y - (2 * s_wh.Height));
								break;
							   // DrawEquation(Gp, ptEqn.X, ptEqn.Y, AxisOptions.Title, AxisOptions.TitleFont, AxisOptions.ABrush)

							case Side.Bottom:
								ptEqn.X = (float)(pmin.X + 0.5 * (pmax.X - pmin.X - s_wh.Width));
								ptEqn.Y = pmin.Y + s_wh.Height + dyy;

								Gp.DrawString(AxisOptions.Title, AxisOptions.TitleFont, AxisOptions.ABrush, (float)(pmin.X + 0.5 * (pmax.X - pmin.X - s_wh.Width)), pmin.Y + s_wh.Height);
								break;
								//DrawEquation(Gp, ptEqn.X, ptEqn.Y, AxisOptions.Title, AxisOptions.TitleFont, AxisOptions.ABrush)
						}
						break;

					case AxisOrientation.Vertical:
						s_wh = Gp.MeasureString(AxisOptions.Title, AxisOptions.TitleFont);
						dx = s_wh.Height;
						Gp.TranslateTransform(pmax.X, pmax.Y);
						Gp.RotateTransform(-90);
						switch (AxisOptions.PlotSide)
						{
							case Side.Left:
								ptEqn.X = (float)(0.5 * (pmax.Y - pmin.Y - s_wh.Width));
								ptEqn.Y = (float)(-s_wh.Height - 1.0 * (s_wh1.Width + s_wh.Height)); // -LLabelW.Max '


								Gp.DrawString(AxisOptions.Title, AxisOptions.TitleFont, AxisOptions.ABrush, ptEqn.X, ptEqn.Y);
								break;
							   // DrawEquation(Gp, ptEqn.X, ptEqn.Y, AxisOptions.Title, AxisOptions.TitleFont, AxisOptions.ABrush)

							case Side.Right:
								ptEqn.X = (float)(0.5 * (pmax.Y - pmin.Y - s_wh.Width));
								if (LLabelW.Count > 0)
								{
									ptEqn.Y = (float)(0.75 * LLabelW.Max() + 2); //(s_wh.Width)
									Gp.DrawString(AxisOptions.Title, AxisOptions.TitleFont, AxisOptions.ABrush, ptEqn.X, ptEqn.Y);
									// DrawEquation(Gp, ptEqn.X, ptEqn.Y, AxisOptions.Title, AxisOptions.TitleFont, AxisOptions.ABrush)
								}
								break;

						}
						Gp.ResetTransform();
						break;
				}
			}

			AxisOptions.ABrush = ABrush;
			AxisOptions.PenMajorLine = penLine;
			AxisOptions.PenMinorLine = penLine;
		}





	}

}
