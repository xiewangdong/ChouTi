using ChouTi.Model;
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using Microsoft.Win32;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using NPOI.XWPF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace ChouTi.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private string randAnswer;

        public string RandAnswer
        {
            get { return randAnswer; }
            set { randAnswer = value; RaisePropertyChanged("RandAnswer"); }
        }

        private int sumCount;

        public int SumCount
        {
            get { return sumCount; }
            set { sumCount = value; RaisePropertyChanged("SumCount"); }
        }

        private int randCount;

        public int RandCount
        {
            get { return randCount; }
            set
            {
                if (value > SumCount)
                {
                    randCount = -1;
                    throw new ArgumentException("抽取数不得大于题库总数！");
                }
                else if (value < 0)
                {
                    randCount = -1;
                    throw new ArgumentException("抽取数不得为负数！");
                }
                else
                {
                    randCount = value;
                }
                RaisePropertyChanged("RandCount");
            }
        }

        public bool SaveAnswer { get; set; }
        public RelayCommand RandomCmd { get; set; }
        public RelayCommand SaveCmd { get; set; }
        public RelayCommand ImportCmd { get; set; }
        public List<Question> QuestionList { get; set; }
        public List<int> RandomOrder { get; set; }

        public MainViewModel()
        {
            QuestionList = new List<Question>();
            RandomOrder = new List<int>();
            RandomCmd = new RelayCommand(new System.Action(DoRandom), () => (QuestionList.Count > 0&&RandCount!=-1));
            SaveCmd = new RelayCommand(new System.Action(Save), () => RandomOrder.Count > 0);
            ImportCmd = new RelayCommand(new System.Action(Import));
            SaveAnswer = false;
        }

        private void Import()
        {
            OpenFileDialog dialog = new OpenFileDialog();
            dialog.DefaultExt = ".xls"; // Default file extension
            dialog.Filter = "Excel 2003 文档|*.xls|Excel 2007 文档|*.xlsx|所有文件|*.*"; // Filter files by extension
            if (dialog.ShowDialog() == true)
            {
                string filename = dialog.FileName;

                string fileType = Path.GetExtension(filename).ToLower();
                try
                {
                    ISheet sheet = null;
                    int sheetNumber = 0;
                    FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read);
                    if (fileType == ".xlsx")
                    {
                        // 2007版本
                        XSSFWorkbook workbook = new XSSFWorkbook(fs);
                        sheet = workbook.GetSheetAt(0);
                        if (sheet != null)
                        {
                            QuestionList.Clear();
                            int count = sheet.LastRowNum;
                            for (int i = 1; i <= count; i++)
                            {
                                QuestionList.Add(new Question()
                                {
                                    Answer = sheet.GetRow(i).GetCell(1).ToString(),
                                    Subject = sheet.GetRow(i).GetCell(0).ToString()
                                });
                            }
                            RandCount = SumCount = QuestionList.Count;
                        }
                    }
                    else if (fileType == ".xls")
                    {
                        // 2003版本
                        HSSFWorkbook workbook = new HSSFWorkbook(fs);
                        sheetNumber = workbook.NumberOfSheets;
                        sheet = workbook.GetSheetAt(0);
                        if (sheet != null)
                        {
                            QuestionList.Clear();
                            int count = sheet.LastRowNum;
                            for (int i = 1; i <= count; i++)
                            {
                                QuestionList.Add(new Question()
                                {
                                    Answer = sheet.GetRow(i).GetCell(1).ToString(),
                                    Subject = sheet.GetRow(i).GetCell(0).ToString()
                                });
                            }
                            RandCount = SumCount = QuestionList.Count;
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }
        }
        private void Save()
        {
            XWPFDocument MyDoc = new XWPFDocument();

            XWPFParagraph p1 = MyDoc.CreateParagraph();
            p1.Alignment = ParagraphAlignment.CENTER; //字体居中
            //创建run对象
            //本节提到的所有样式都是基于XWPFRun的，
            //你可以把XWPFRun理解成一小段文字的描述对象，
            //这也是Word文档的特征，即文本描述性文档。
            //来自Tony Qu http://tonyqus.sinaapp.com/archives/609
            var runTitle = p1.CreateRun();
            runTitle.IsBold = true;
            runTitle.SetText("试题");
            runTitle.FontSize = 20;
            runTitle.SetFontFamily("方正小标宋", FontCharRange.None);
            runTitle.AddCarriageReturn();

            int index = 1;
            foreach (int item in RandomOrder)
            {
                //创建段落对象2
                var p = MyDoc.CreateParagraph();
                var run = p.CreateRun();
                string text = index + "." + QuestionList[item].Subject;
                run.SetText(text);
                run.FontSize = 16;
                run.SetFontFamily("仿宋GB_2312", FontCharRange.None);

                //设置答案
                var ap = MyDoc.CreateParagraph();
                var arun = ap.CreateRun();
                if (SaveAnswer)
                {
                    string answer = "答：" + QuestionList[item].Answer;
                    arun.SetText(answer);
                }
                else
                {
                    arun.AddCarriageReturn();
                    arun.AddCarriageReturn();
                    arun.AddCarriageReturn();
                }
                arun.FontSize = 16;
                arun.SetFontFamily("仿宋GB_2312", FontCharRange.None);

                index++;
            }

            SaveFileDialog sfd = new SaveFileDialog();
            //设置文件类型 
            sfd.Filter = "Word2007 文档|*.docx";

            //设置默认文件类型显示顺序 
            sfd.FilterIndex = 1;

            //保存对话框是否记忆上次打开的目录 
            sfd.RestoreDirectory = true;

            //点了保存按钮进入 
            if (sfd.ShowDialog() == true)
            {
                string localFilePath = sfd.FileName.ToString(); //获得文件路径 

                MemoryStream ms = new MemoryStream();
                //开始写入
                MyDoc.Write(ms);

                using (FileStream fs = new FileStream(localFilePath, FileMode.Create, FileAccess.Write))
                {
                    byte[] data = ms.ToArray();
                    fs.Write(data, 0, data.Length);
                    fs.Flush();
                }
                ms.Close();
            }
        }

        private void DoRandom()
        {
            Random random = new Random((int)DateTime.Now.Ticks & 0x0000FFFF);
            List<int> list = new List<int>();
            RandomOrder.Clear();
            for (int i = 0; i < QuestionList.Count; i++)
            {
                list.Add(i);
            }
            for (int j = 0; j < RandCount; j++)
            {
                int index = random.Next(0, list.Count);
                RandomOrder.Add(list[index]);
                list.RemoveAt(index);
            }
            RandAnswer = string.Empty;
            int questionIndex = 1;
            foreach (int item in RandomOrder)
            {
                RandAnswer += questionIndex + ". " + QuestionList[item].Subject + "\r\n\r\n";
                questionIndex++;
            }
        }
    }
}
