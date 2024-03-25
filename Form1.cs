using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Windows.Forms.DataVisualization.Charting;
using System.Xml;
using System.Globalization;
using System.IO;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace GPX
{
    public partial class Form1 : Form
    {
        private string filePath;
        private List<Tuple<double, double, double, DateTime>> utmCoordinates;
        private List<Tuple<double, double, DateTime>> wptCoordinates;

        public Form1()
        {
            InitializeComponent();
            utmCoordinates = new List<Tuple<double, double, double, DateTime>>();
            wptCoordinates = new List<Tuple<double, double, DateTime>>();

        }

        // Обработчик события нажатия кнопки "Выбрать файл"
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                filePath = openFileDialog.FileName;
            }
        }

        // Обработчик события нажатия кнопки "Обработать файл"
        private void button2_Click(object sender, EventArgs e)
        {
            // Сброс масштаба графика
            chart.ChartAreas[0].AxisX.ScaleView.ZoomReset();
            chart.ChartAreas[0].AxisY.ScaleView.ZoomReset();

            if (string.IsNullOrEmpty(filePath))
            {
                MessageBox.Show("Файл не выбран.");
                return;
            }

            utmCoordinates.Clear();
            listBox1.Items.Clear(); // Очищаем ListBox перед началом загрузки новых данных

            XmlDocument doc = new XmlDocument();
            doc.Load(filePath);

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(doc.NameTable);
            nsmgr.AddNamespace("gpx", "http://www.topografix.com/GPX/1/1");

            // Обработка точек trkpt
            XmlNodeList trkptNodes = doc.SelectNodes("//gpx:trkpt", nsmgr);
            // Обработка точек wpt
            XmlNodeList wptNodes = doc.SelectNodes("//gpx:wpt", nsmgr);

            // Обработка точек trkpt
            foreach (XmlNode trkptNode in trkptNodes)
            {
                string latString = trkptNode.Attributes["lat"].Value;
                string lonString = trkptNode.Attributes["lon"].Value;
                string timeString = trkptNode.SelectSingleNode("gpx:time", nsmgr).InnerText;
                string eleString = trkptNode.SelectSingleNode("gpx:ele", nsmgr).InnerText;

                double latitude = double.Parse(latString, CultureInfo.InvariantCulture);
                double longitude = double.Parse(lonString, CultureInfo.InvariantCulture);
                DateTime date = DateTime.ParseExact(timeString, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);
                double elevation = double.Parse(eleString, CultureInfo.InvariantCulture);

                int zone;
                double easting, northing;

                DegreesToUTM(longitude, latitude, out easting, out northing, out zone);

                // Округляем координаты до целых чисел
                int roundedEasting = (int)Math.Round(easting);
                int roundedNorthing = (int)Math.Round(northing);

                utmCoordinates.Add(new Tuple<double, double, double, DateTime>(easting, northing, elevation, date));

                // Вывод информации в ListBox
                listBox1.Items.Add($"({roundedEasting}, {roundedNorthing}) {date.ToString("yyyy-MM-dd HH:mm:ss")}");
            }

            chart.Series.Clear();
            Series series = chart.Series.Add("UTM Coordinates");
            series.ChartType = SeriesChartType.Line;

            // Находим минимальные и максимальные значения координат
            double minEasting = utmCoordinates.Min(c => c.Item1);
            double minNorthing = utmCoordinates.Min(c => c.Item2);
            double maxEasting = utmCoordinates.Max(c => c.Item1);
            double maxNorthing = utmCoordinates.Max(c => c.Item2);

            // Устанавливаем границы графика
            chart.ChartAreas[0].AxisX.Minimum = minEasting;
            chart.ChartAreas[0].AxisY.Minimum = minNorthing;
            chart.ChartAreas[0].AxisX.Maximum = maxEasting;
            chart.ChartAreas[0].AxisY.Maximum = maxNorthing;

            // Настройка формата для оси X
            chart.ChartAreas[0].AxisX.LabelStyle.Format = "F0";

            // Настройка формата для оси Y
            chart.ChartAreas[0].AxisY.LabelStyle.Format = "F0";


            // Добавляем точки на график
            foreach (var utmCoordinate in utmCoordinates)
            {
                series.Points.AddXY(utmCoordinate.Item1, utmCoordinate.Item2);
            }

            // Очищаем список координат точек wpt перед началом загрузки новых данных
            wptCoordinates.Clear();
            listBox2.Items.Clear(); // Очищаем ListBox перед началом загрузки новых данных
            textBox1.Visible = true;
            listBox2.Visible = true;

            // Обработка точек wpt
            if (wptNodes.Count == 0)
            {
                listBox2.Visible = false;
                textBox1.Visible = false;
            }
            else
            {
                foreach (XmlNode wptNode in wptNodes)
                {
                    string latString = wptNode.Attributes["lat"].Value;
                    string lonString = wptNode.Attributes["lon"].Value;
                    XmlNode timeNode = wptNode.SelectSingleNode("gpx:time", nsmgr);
                    string timeString = timeNode != null ? timeNode.InnerText : null;

                    if (double.TryParse(latString, NumberStyles.Any, CultureInfo.InvariantCulture, out double latitude) &&
                        double.TryParse(lonString, NumberStyles.Any, CultureInfo.InvariantCulture, out double longitude) &&
                        DateTime.TryParseExact(timeString, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out DateTime date))
                    {
                        int zone;
                        double easting, northing;

                        DegreesToUTM(longitude, latitude, out easting, out northing, out zone);

                        // Округляем координаты до целых чисел
                        int roundedEasting = (int)Math.Round(easting);
                        int roundedNorthing = (int)Math.Round(northing);

                        wptCoordinates.Add(new Tuple<double, double, DateTime>(roundedEasting, roundedNorthing, date));

                        // Вывод информации в ListBox
                        listBox2.Items.Add($"({roundedEasting}, {roundedNorthing}) {date.ToString("yyyy-MM-dd HH:mm:ss")}");
                    }
                    else
                    {
                        // Обработка ошибок парсинга даты или координат
                        listBox2.Items.Add("Ошибка при обработке точки wpt");
                    }
                }

                // Сброс масштаба графика
                chart.ChartAreas[0].AxisX.ScaleView.ZoomReset();
                chart.ChartAreas[0].AxisY.ScaleView.ZoomReset();

                // Добавляем точки на график
                Series series2 = chart.Series.FindByName("Waypoints");
                if (series2 == null)
                {
                    series2 = chart.Series.Add("Waypoints");
                    series2.ChartType = SeriesChartType.Point;
                }
                else
                {
                    series2.Points.Clear();
                }

                foreach (var wptCoordinate in wptCoordinates)
                {
                    series2.Points.AddXY(wptCoordinate.Item1, wptCoordinate.Item2);
                }
            }
        }

        // Константы для WGS84 эллипсоида
        private const double WGS84_A = 6378137.0; // Полуось эллипсоида
        private const double WGS84_F = 1 / 298.257223563; // Сжатие эллипсоида
        private const double UTM_K0 = 0.9996; // Масштабный коэффициент
        private const double UTM_FE = 500000.0; // False Easting
        private const double K0 = 0.9996; // Масштабный коэффициент для проекции UTM

        // Метод для преобразования географических координат в UTM
        public static void DegreesToUTM(double lng, double lat, out double X, out double Y, out int zoneNumber)
        {
            // Убедитесь, что долгота находится между -180.00 .. 179.9
            double LongTemp = (lng + 180) - (int)((lng + 180) / 360) * 360 - 180;

            // Вычислить номер зоны
            zoneNumber = (int)((LongTemp + 180) / 6) + 1;

            // Вычислить центральный меридиан для зоны
            double LongOrigin = (zoneNumber - 1) * 6 - 180 + 3;

            // Преобразовать градусы в радианы
            double LatRad = lat * Math.PI / 180;
            double LongRad = LongTemp * Math.PI / 180;
            double LongOriginRad = LongOrigin * Math.PI / 180;

            // Вычислить UTM значения
            double N = WGS84_A / Math.Sqrt(1 - WGS84_F * Math.Sin(LatRad) * Math.Sin(LatRad));
            double T = Math.Tan(LatRad) * Math.Tan(LatRad);
            double C = WGS84_F * WGS84_F * Math.Cos(LatRad) * Math.Cos(LatRad);
            double A = Math.Cos(LatRad) * (LongRad - LongOriginRad);

            double M = WGS84_A * ((1 - WGS84_F / 4 - 3 * WGS84_F * WGS84_F / 64 - 5 * WGS84_F * WGS84_F * WGS84_F / 256) * LatRad
                - (3 * WGS84_F / 8 + 3 * WGS84_F * WGS84_F / 32 + 45 * WGS84_F * WGS84_F * WGS84_F / 1024) * Math.Sin(2 * LatRad)
                + (15 * WGS84_F * WGS84_F / 256 + 45 * WGS84_F * WGS84_F * WGS84_F / 1024) * Math.Sin(4 * LatRad)
                - (35 * WGS84_F * WGS84_F * WGS84_F / 3072) * Math.Sin(6 * LatRad));

            X = UTM_FE + UTM_K0 * N * (A + (1 - T + C) * Math.Pow(A, 3) / 6
                + (5 - 18 * T + Math.Pow(T, 2) + 72 * C - 58 * WGS84_F) * Math.Pow(A, 5) / 120);
            Y = K0 * (M + N * Math.Tan(LatRad) * (Math.Pow(A, 2) / 2
                + (5 - T + 9 * C + 4 * Math.Pow(C, 2)) * Math.Pow(A, 4) / 24
                + (61 - 58 * T + Math.Pow(T, 2) + 600 * C - 330 * WGS84_F) * Math.Pow(A, 6) / 720));

            if (lat < 0)
                Y += 10000000; // 10000000 метров смещение для южного полушария
        }

        private void button4_Click(object sender, EventArgs e)
        {
           
            ZoomChart(1.1); // Увеличить на 10%
        }

        private void button3_Click(object sender, EventArgs e)
        {
         
            ZoomChart(0.9); // Уменьшить на 10%
        }

        private void ZoomChart(double zoomFactor)
        {
            var chartArea = chart.ChartAreas[0];
            var xAxis = chartArea.AxisX;
            var yAxis = chartArea.AxisY;

            // Вычислить центр текущего вида
            double xCenter = (xAxis.ScaleView.ViewMinimum + xAxis.ScaleView.ViewMaximum) / 2;
            double yCenter = (yAxis.ScaleView.ViewMinimum + yAxis.ScaleView.ViewMaximum) / 2;

            // Вычислить новый размер вида по оси X и Y
            double xWidth = (xAxis.ScaleView.ViewMaximum - xAxis.ScaleView.ViewMinimum) * zoomFactor;
            double yHeight = (yAxis.ScaleView.ViewMaximum - yAxis.ScaleView.ViewMinimum) * zoomFactor;

            // Вычислить новые минимальные и максимальные значения для осей
            double xMin = xCenter - xWidth / 2;
            double xMax = xCenter + xWidth / 2;
            double yMin = yCenter - yHeight / 2;
            double yMax = yCenter + yHeight / 2;

            // Применить новый уровень приближения к осям
            xAxis.ScaleView.Zoom(xMin, xMax);
            yAxis.ScaleView.Zoom(yMin, yMax);
        }

        // Обработчик события нажатия кнопки "Сохранить координаты в файл"
        private void button5_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Текстовые файлы (*.txt)|*.txt|Все файлы (*.*)|*.*";
            saveFileDialog.Title = "Сохранить файл с координатами";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter writer = new StreamWriter(saveFileDialog.FileName))
                {
                    foreach (var utmCoordinate in utmCoordinates)
                    {
                        writer.WriteLine($"{utmCoordinate.Item1.ToString(CultureInfo.InvariantCulture)}     \t{utmCoordinate.Item2.ToString(CultureInfo.InvariantCulture)}     \t{utmCoordinate.Item3.ToString(CultureInfo.InvariantCulture)}     \t{utmCoordinate.Item4}");
                    }
                }

                MessageBox.Show("Координаты успешно сохранены в файл.", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}