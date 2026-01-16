using Newtonsoft.Json;
using Serilog;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web.UI.WebControls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace MSSQL
{
    #region Config Class
    public class SerialNumber
    {
        [JsonProperty("Database_Name_ForShowInformation_val")]
        public string Database_Name_ForShowInformation_val { get; set; }
        [JsonProperty("Table_Name_ForShowInformation_val")]
        public string Table_Name_ForShowInformation_val { get; set; }
        [JsonProperty("Database_Name_ForOperateDatabase_val")]
        public string Database_Name_ForOperateDatabase_val { get; set; }
        [JsonProperty("Database_Name_ForOperateTable_val")]
        public string Database_Name_ForOperateTable_val { get; set; }
        [JsonProperty("Table_Name_ForOperateTable_val")]
        public string Table_Name_ForOperateTable_val { get; set; }
        [JsonProperty("Dialogue_val")]
        public string Dialogue_val { get; set; }
        [JsonProperty("Server_Name_val")]
        public string Server_Name_val { get; set; }

    }

    public class Model
    {
        [JsonProperty("SerialNumbers")]
        public SerialNumber SerialNumbers { get; set; }
    }

    public class RootObject
    {
        [JsonProperty("Models")]
        public List<Model> Models { get; set; }
    }
    #endregion

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        #region Function
        private void WindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (MessageBox.Show("請問是否要關閉？", "確認", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                if (sql == null) return;
                sql.Disconnect();
                e.Cancel = false;
            }
            else
            {
                e.Cancel = true;
            }
        }

        #region Config
        private SerialNumber SerialNumberClass()
        {
            SerialNumber serialnumber_ = new SerialNumber
            {
                Database_Name_ForShowInformation_val = Database_Name_ForShowInformation.Text,
                Table_Name_ForShowInformation_val = Table_Name_ForShowInformation.Text,
                Database_Name_ForOperateDatabase_val = Database_Name_ForOperateDatabase.Text,
                Database_Name_ForOperateTable_val = Database_Name_ForOperateTable.Text,
                Table_Name_ForOperateTable_val = Table_Name_ForOperateTable.Text,
                Dialogue_val = new TextRange(Dialogue.Document.ContentStart, Dialogue.Document.ContentEnd).Text.TrimEnd('\r', '\n'),
                Server_Name_val = Server_Name.Text,
            };
            return serialnumber_;
        }

        private void LoadConfig(int model, int serialnumber, bool encryption = false)
        {
            List<RootObject> Parameter_info = config.Load(encryption);
            if (Parameter_info != null)
            {
                Database_Name_ForShowInformation.Text = Parameter_info[model].Models[serialnumber].SerialNumbers.Database_Name_ForShowInformation_val;
                Table_Name_ForShowInformation.Text = Parameter_info[model].Models[serialnumber].SerialNumbers.Table_Name_ForShowInformation_val;
                Database_Name_ForOperateDatabase.Text = Parameter_info[model].Models[serialnumber].SerialNumbers.Database_Name_ForOperateDatabase_val;
                Database_Name_ForOperateTable.Text = Parameter_info[model].Models[serialnumber].SerialNumbers.Database_Name_ForOperateTable_val;
                Table_Name_ForOperateTable.Text = Parameter_info[model].Models[serialnumber].SerialNumbers.Table_Name_ForOperateTable_val;
                Dialogue.Document.Blocks.Clear();
                Dialogue.Document.Blocks.Add(new Paragraph(new Run(Parameter_info[model].Models[serialnumber].SerialNumbers.Dialogue_val)));
                Server_Name.Text = Parameter_info[model].Models[serialnumber].SerialNumbers.Server_Name_val;
                Log.Information("導入參數。");
            }
            else
            {
                // 結構:2個Models、Models下在各2個SerialNumbers
                SerialNumber serialnumber_ = SerialNumberClass();
                List<Model> models = new List<Model>
                {
                    new Model { SerialNumbers = serialnumber_ },
                    new Model { SerialNumbers = serialnumber_ }
                };
                List<RootObject> rootObjects = new List<RootObject>
                {
                    new RootObject { Models = models },
                    new RootObject { Models = models }
                };
                config.SaveInit(rootObjects, encryption);
            }
        }

        private void SaveConfig(int model, int serialnumber, bool encryption = false)
        {
            config.Save(model, serialnumber, SerialNumberClass(), encryption);
            Log.Information("儲存參數。");
        }
        #endregion

        #region Dispatcher Invoke 
        public string DispatcherGetValue(System.Windows.Controls.TextBox control)
        {
            string content = "";
            this.Dispatcher.Invoke(() =>
            {
                content = control.Text;
            });
            return content;
        }

        public void DispatcherSetValue(string content, System.Windows.Controls.TextBox control)
        {
            this.Dispatcher.Invoke(() =>
            {
                control.Text = content;
            });
        }

        #region IntegerUpDown Invoke
        //public int? DispatcherIntegerUpDownGetValue(Xceed.Wpf.Toolkit.IntegerUpDown control)
        //{
        //    int? content = null;
        //    this.Dispatcher.Invoke(() =>
        //    {
        //        if (int.TryParse(control.Text, out int result))
        //        {
        //            content = result;
        //        }
        //        else
        //        {
        //            content = null;
        //        }
        //    });
        //    return content;
        //}
        #endregion
        #endregion

        /// <summary>
        /// Log.Information("Application started at {time}", DateTime.Now);<br/>
        /// Log.Warning("Low disk space on drive C:");<br/>
        /// Log.Error("Unhandled exception: {exception}", new Exception("Test error"));<br/>
        /// Log.Debug("Debug 訊息");<br/>
        /// </summary>
        private void LoggerInit()
        {
            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File("LogRecord/Log-.txt", rollingInterval: RollingInterval.Day)
               .WriteTo.Sink(new RichTextBoxSink(richTextBoxDebug, richTextBoxGeneral, richTextBoxWarning, richTextBoxError, LogRecord))
               .CreateLogger();
        }

        private void WriteVersionToXml()
        {
            // 取得程式名稱（不含副檔名）
            string appName = Assembly.GetEntryAssembly()?.GetName().Name ?? "UnknownApp";
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;  // 執行檔目錄
            string assemblyInfoPath = System.IO.Path.GetFullPath(System.IO.Path.Combine(baseDir, @"..\..\..\Properties\AssemblyInfo.cs"));
            if (File.Exists(assemblyInfoPath))
            {
                // 讀取 AssemblyInfo.cs
                string content = File.ReadAllText(assemblyInfoPath);
                // 使用正則抓取 AssemblyFileVersion
                Regex regex = new Regex(@"\[assembly:\s*AssemblyFileVersion\s*\(\s*""(?<version>[\d\.]+)""\s*\)\s*\]");
                Match match = regex.Match(content);
                if (match.Success)
                {
                    string versionStr = match.Groups["version"].Value; // 例如 "1.2.3.45"
                    // 分割版本號
                    string[] parts = versionStr.Split('.');
                    string major = parts.Length > 0 ? parts[0] : "0";
                    string minor = parts.Length > 1 ? parts[1] : "0";
                    string patch = parts.Length > 2 ? parts[2] : "0";
                    string build = parts.Length > 3 ? parts[3] : "0";
                    // 建立 XML
                    XDocument doc = new XDocument(
                        new XDeclaration("1.0", "utf-8", null),
                        new XElement("VersionInfo",
                            new XElement("Application",
                                new XAttribute("name", appName),
                                new XElement("Version",
                                    new XAttribute("major", major),
                                    new XAttribute("minor", minor),
                                    new XAttribute("patch", patch),
                                    new XAttribute("build", build)
                                )
                            )
                        )
                    );
                    // 寫入 XML 檔案
                    string outputPath = "AssemblyVersion.xml";
                    doc.Save(outputPath);
                }
            }
        }

        private void OpenFolder(string description, System.Windows.Controls.TextBox textbox)
        {
            System.Windows.Forms.FolderBrowserDialog path = new System.Windows.Forms.FolderBrowserDialog();
            path.Description = description;
            path.ShowDialog();
            textbox.Text = path.SelectedPath;
            Log.Warning("開啟資料夾路徑︰{Path}!", path.SelectedPath);
        }

        private bool WarnAndLog(string name, string recordName)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Log.Warning("請輸入{recordName}!", recordName);
                MessageBox.Show($"請輸入{recordName}!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }
            return false;
        }
        #endregion

        #region Parameter and Init
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            LoggerInit();
            WriteVersionToXml();
            LoadConfig(0, 0);
        }
        BaseConfig<RootObject> config = new BaseConfig<RootObject>();
        MSSQLHandler sql;
        #endregion

        #region Main Screen
        private void Main_Btn_Click(object sender, RoutedEventArgs e)
        {
            switch ((sender as System.Windows.Controls.Button).Name)
            {
                case nameof(Connet):
                    {
                        if (WarnAndLog(Server_Name.Text, "Server Name")) return;
                        sql = new MSSQLHandler(Server_Name.Text); // "localhost\MSSQL2022"
                        sql.Connect();
                        //RedisHelper.DB.StringSet("Machine:N7-15:Status", "Running");
                        //RedisHelper.DB.StringSet("Machine:N7-15:OK", 1234);
                        break;
                    }
                case nameof(Disconnet):
                    {
                        sql.Disconnect();
                        //string status = RedisHelper.DB.StringGet("Machine:N7-15:Status");
                        //int okCount = (int)RedisHelper.DB.StringGet("Machine:N7-15:OK");
                        //Console.WriteLine(status);
                        //Console.WriteLine(okCount);
                        break;
                    }
                case nameof(Save_Config):
                    {
                        SaveConfig(0, 0);
                        break;
                    }
                case nameof(Show_Database):
                    {
                        sql.DatabaseOrTableInform("", "", sql.ShowDatabase);
                        break;
                    }
                case nameof(Show_Table):
                    {
                        string databaseName = Database_Name_ForShowInformation.Text;
                        if (WarnAndLog(databaseName, "Database Name")) return;
                        sql.DatabaseOrTableInform(databaseName, "", sql.ShowTable);
                        break;
                    }
                case nameof(Show_TableData):
                    {
                        string databaseName = Database_Name_ForShowInformation.Text;
                        string tableName = Table_Name_ForShowInformation.Text;
                        if (WarnAndLog(databaseName, "Database Name")) return;
                        if (WarnAndLog(tableName, "Table Name")) return;
                        sql.DatabaseOrTableInform(databaseName, tableName, sql.ShowTableData);
                        break;
                    }
                case nameof(Create_Database):
                    {
                        string databaseName = Database_Name_ForOperateDatabase.Text;
                        if (WarnAndLog(databaseName, "Database Name")) return;
                        sql.DatabaseOperate(databaseName, sql.CreateDatabase);
                        break;
                    }
                case nameof(Drop_Database):
                    {
                        string databaseName = Database_Name_ForOperateDatabase.Text;
                        if (WarnAndLog(databaseName, "Database Name")) return;
                        sql.DatabaseOperate(databaseName, sql.DropDatabase);
                        break;
                    }
                case nameof(Create_Table):
                    {
                        string databaseName = Database_Name_ForOperateTable.Text;
                        string tableName = Table_Name_ForOperateTable.Text;
                        string content = new TextRange(Dialogue.Document.ContentStart, Dialogue.Document.ContentEnd).Text.Replace("\r\n", "");
                        if (WarnAndLog(databaseName, "Database Name")) return;
                        if (WarnAndLog(tableName, "Table Name")) return;
                        if (WarnAndLog(content, "content")) return;
                        sql.TableOperate(databaseName, tableName, content, sql.CreateTable);
                        break;
                    }
                case nameof(Drop_Table):
                    {
                        string databaseName = Database_Name_ForOperateTable.Text;
                        string tableName = Table_Name_ForOperateTable.Text;
                        string content = new TextRange(Dialogue.Document.ContentStart, Dialogue.Document.ContentEnd).Text.Replace("\r\n", "");
                        if (WarnAndLog(databaseName, "Database Name")) return;
                        if (WarnAndLog(tableName, "Table Name")) return;
                        if (WarnAndLog(content, "content")) return;
                        sql.TableOperate(databaseName, tableName, content, sql.DropTable);
                        break;
                    }
                case nameof(Insert_Data):
                    {
                        string databaseName = Database_Name_ForOperateTable.Text;
                        string tableName = Table_Name_ForOperateTable.Text;
                        string content = new TextRange(Dialogue.Document.ContentStart, Dialogue.Document.ContentEnd).Text.Replace("\r\n", "");
                        if (WarnAndLog(databaseName, "Database Name")) return;
                        if (WarnAndLog(tableName, "Table Name")) return;
                        if (WarnAndLog(content, "content")) return;
                        sql.TableOperate(databaseName, tableName, content, sql.InsertData);
                        break;
                    }
                case nameof(Update_Data):
                    {
                        string databaseName = Database_Name_ForOperateTable.Text;
                        string tableName = Table_Name_ForOperateTable.Text;
                        string content = new TextRange(Dialogue.Document.ContentStart, Dialogue.Document.ContentEnd).Text.Replace("\r\n", "");
                        if (WarnAndLog(databaseName, "Database Name")) return;
                        if (WarnAndLog(tableName, "Table Name")) return;
                        if (WarnAndLog(content, "content")) return;
                        sql.TableOperate(databaseName, tableName, content, sql.UpdateData);
                        break;
                    }
                case nameof(Delete_Data):
                    {
                        string databaseName = Database_Name_ForOperateTable.Text;
                        string tableName = Table_Name_ForOperateTable.Text;
                        string content = new TextRange(Dialogue.Document.ContentStart, Dialogue.Document.ContentEnd).Text.Replace("\r\n", "");
                        if (WarnAndLog(databaseName, "Database Name")) return;
                        if (WarnAndLog(tableName, "Table Name")) return;
                        if (WarnAndLog(content, "content")) return;
                        sql.TableOperate(databaseName, tableName, content, sql.DeleteData);
                        break;
                    }
            }
        }

        private void About_Click(object sender, MouseButtonEventArgs e)
        {
            string filePath = "AssemblyVersion.xml";
            if (!File.Exists(filePath))
            {
                MessageBox.Show("未找到版本號 XML!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                XDocument doc = XDocument.Load(filePath);
                XElement versionElement = doc.Root?.Element("Application")?.Element("Version");
                if (versionElement != null)
                {
                    string major = versionElement.Attribute("major")?.Value ?? "0";
                    string minor = versionElement.Attribute("minor")?.Value ?? "0";
                    string patch = versionElement.Attribute("patch")?.Value ?? "0";
                    string build = versionElement.Attribute("build")?.Value ?? "0";
                    string version = $"{major}.{minor}.{patch}.{build}";
                    MessageBox.Show($"版本號︰{version}", "版本", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show("XML 中未找到版本號!", "警告", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"讀取版本號失敗: {ex.Message}", "錯誤", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            e.Handled = true; // 阻止切換到這個 Tab 的內容
        }
        #endregion


    }
}
