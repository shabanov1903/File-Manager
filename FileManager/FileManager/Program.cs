using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Text;

namespace FileManager
{
    class Program
    {
        // Текущая директория
        private static string workDir;
        // Рассматриваемый файл
        private static string reqDir;
        // Проверка на запрос
        private static bool req;
        // Количество строк на странице
        private static int stringsInPage;
        // Текущий номер страницы
        private static int pageNumber;
        // Массив директорий и файлов
        private static List<string> directoryMass = new List<string>();
        // Объект хранения данных
        private static Data data = new Data();

        // Путь для сохранения ошибок
        private static string errDir;

        static void Main(string[] args)
        {
            // Блок инициализации по количеству выводимых строк
            try
            {
                stringsInPage = Int32.Parse(ConfigurationManager.AppSettings.Get("stringsInPage"));
            }
            catch (Exception e)
            {
                stringsInPage = 20;
                WriteError($"{DateTime.Now.ToString()}: {e.Message}");
            }

            // Блок инициализации по начальной директории
            if (ConfigurationManager.AppSettings.Get("startDirectory") == "null")
            {
                workDir = Directory.GetCurrentDirectory();
            }
            else
            {
                workDir = ConfigurationManager.AppSettings.Get("startDirectory");
            }

            // Директория для хранения файла ошибок
            errDir = $@"{Directory.GetCurrentDirectory()}\Errors";
            if (Directory.Exists(errDir) == false) Directory.CreateDirectory(errDir);

            while (true)
            {
                GetFileList(new System.IO.DirectoryInfo(workDir));
                UIFunction();
                var command = Console.ReadLine();
                Parser(command);
            }
        }

        // Функция для заполнения списка по заданной директории
        static void GetFileList(System.IO.DirectoryInfo root)
        {
            // Очистка списка перед новым заполнением
            directoryMass.Clear();

            // Массив файлов в текущей директории
            System.IO.FileInfo[] files = null;
            // Массив папок в текущей директории
            System.IO.DirectoryInfo[] subDirs = null;

            // Получение всех файлов в директории
            try
            {
                files = root.GetFiles();
                subDirs = root.GetDirectories();
            }
            // В случае ошибки доступа вывести в консоль сообщение
            catch (UnauthorizedAccessException e)
            {
                WriteError($"{DateTime.Now.ToString()}: {e.Message}");
            }
            // В случае отсутствия директории вывести в консоль сообщение
            catch (System.IO.DirectoryNotFoundException e)
            {
                WriteError($"{DateTime.Now.ToString()}: {e.Message}");
            }

            if (subDirs != null)
            {
                // Последовательный перебор всех папок в директории и добавление элемента в статический лист
                foreach (System.IO.DirectoryInfo subDir in subDirs)
                {
                    directoryMass.Add(subDir.Name);
                }
            }
            if (files != null)
            {
                // Последовательный перебор всех файлов в директории и добавление элемениа в статический лист
                foreach (System.IO.FileInfo file in files)
                {
                    directoryMass.Add(file.Name);
                }
            }
        }

        // Функция для получения сведений о файле / директории
        static void Information(string path)
        {
            //Data data = new Data();
            if (File.Exists(path))
            {
                // Обработка файла
                var fileInfo = new FileInfo(path);
                data.name = fileInfo.Name;
                data.creationTime = Convert.ToString(fileInfo.CreationTime);
                data.lastWriteTime = Convert.ToString(fileInfo.LastWriteTime);
                data.size = Convert.ToString(fileInfo.Length);
                data.path = Convert.ToString(fileInfo.FullName);
            }
            else if (Directory.Exists(path))
            {
                // Обработка директории
                var directoryInfo = new DirectoryInfo(path);
                data.name = directoryInfo.Name;
                data.creationTime = Convert.ToString(directoryInfo.CreationTime);
                data.lastWriteTime = Convert.ToString(directoryInfo.LastWriteTime);
                data.size = Convert.ToString(DirSize(new DirectoryInfo(path)));
                data.path = Convert.ToString(directoryInfo.FullName);
            }
            else
            {
                WriteError($"{DateTime.Now.ToString()}: Задан некорректный путь к файлу или папке!");
            }
        }

        // Функция для разбора команд
        static void Parser(string command)
        {
            string[] arguments = command.Split(" --");
            switch (arguments[0])
            {
                case "copy":
                    {
                        F_Copy(arguments[1], arguments[2]);
                    }; break;
                case "del":
                    {
                        F_Delete(arguments[1]);
                    }; break;
                case "cd":
                    {
                        workDir = arguments[1];
                        System.Configuration.Configuration config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                        config.AppSettings.Settings["startDirectory"].Value = workDir;
                        config.Save(ConfigurationSaveMode.Modified);
                        req = false;
                    }; break;
                case "info":
                    {
                        reqDir = arguments[1];
                        req = true;
                    }; break;
                case "page":
                    {
                        int sumPage = (int) (directoryMass.Count / stringsInPage);
                        if (sumPage == 1) sumPage = 2;
                        if (arguments[1] == "next")
                        {
                            if (pageNumber != (sumPage - 1)) pageNumber++;
                        }
                        if (arguments[1] == "prev")
                        {
                            if (pageNumber != 0) pageNumber--;
                        }
                    }; break;
                case "update":
                    {
                        pageNumber = 0;
                        req = false;
                    }; break;
                default:
                    {
                        WriteError($"{DateTime.Now.ToString()}: Неверный формат команды!");
                    }; break;
            }
        }

        // Функция копирования
        static void F_Copy(string objForCopy, string pathForCopy)
        {
            if (File.Exists(objForCopy))
            {
                // Обработка файла
                try
                {
                    StringBuilder fullPath = new StringBuilder();
                    var fileInfo = new FileInfo(objForCopy);
                    fullPath.Append(pathForCopy);
                    fullPath.Append("/");
                    fullPath.Append(fileInfo.Name);
                    File.Copy(objForCopy, fullPath.ToString());
                }
                catch (Exception)
                {
                    WriteError($"{DateTime.Now.ToString()}: Неверные параметры копирования файла!");
                }
            }
            else if (Directory.Exists(objForCopy))
            {
                // Обработка директории
                try
                {
                    DirectoryCopy(objForCopy, pathForCopy);
                }
                catch (Exception)
                {
                    WriteError($"{DateTime.Now.ToString()}: Неверные параметры копирования директории!");
                }
            }
            else
            {
                WriteError($"{DateTime.Now.ToString()}: Задан некорректный путь к файлу или папке!");
            }
        }

        // Функция удаления
        static void F_Delete(string objForDelete)
        {
            if (File.Exists(objForDelete))
            {
                // Обработка файла
                try
                {
                    File.Delete(objForDelete);
                }
                catch (Exception)
                {
                    WriteError($"{DateTime.Now.ToString()}: Неверные параметры для удаления файла!");
                }
            }
            else if (Directory.Exists(objForDelete))
            {
                // Обработка директории
                try
                {
                    Directory.Delete(objForDelete, true);
                }
                catch (Exception)
                {
                    WriteError($"{DateTime.Now.ToString()}: Неверные параметры для удаления директории!");
                }
            }
            else
            {
                WriteError($"{DateTime.Now.ToString()}: Задан некорректный путь к файлу или папке!");
            }
        }

        // Функция для построения UI
        private static void UIFunction()
        {
            Console.Clear();
            Console.WriteLine($"+--------------------------------------------------------FileManager--------------------------------------------------------+");

            StringBuilder fullPath = new StringBuilder();
            fullPath.Append("Directory Path: ");
            fullPath.Append(workDir);
            PrintDirectory(fullPath);

            Console.WriteLine($"+---------------------------------------------------------------------------------------------------------------------------+");
            for (int i = 0; i < stringsInPage; i++)
            {
                bool bPath = false;
                for (int j = 0; j < 125; j++)
                {
                    if (j == 0 || j == 124)
                    {
                        Console.Write("|");
                    }
                    else if (directoryMass.Count > (i + stringsInPage * pageNumber) && bPath == false)
                    {
                        Console.Write(directoryMass[(i + stringsInPage * pageNumber)]);
                        j = directoryMass[(i + stringsInPage * pageNumber)].Length;
                        bPath = true;
                    }
                    else Console.Write(" ");
                }
                Console.WriteLine();
            }

            if (req)
            {
                Information(reqDir);
                fullPath.Append("Target Path: ");
                fullPath.Append(reqDir);
            }
            else
            {
                Information(workDir);
                fullPath.Append("Target Path: ");
                fullPath.Append(workDir);
            }

            Console.WriteLine($"+---------------------------------------------------------------------------------------------------------------------------+");
            for (int i = 0; i < 5; i++)
            {
                switch (i)
                {
                    case 0: PrintDirectory(fullPath); break;
                    case 1: PrintInfo("Name", data.name); break;
                    case 2: PrintInfo("Creation Time", data.creationTime); break;
                    case 3: PrintInfo("Last Write Time", data.lastWriteTime); break;
                    case 4: PrintInfo("Size", data.size); break;
                }
            }

            Console.WriteLine($"+---------------------------------------------------------------------------------------------------------------------------+");
            Console.WriteLine($"Enter The Command:");
        }

        // Функция для копирования каталога
        private static void DirectoryCopy(string sourceDirName, string destDirName)
        {
            // Получение списка дочерних директорий
            DirectoryInfo dir = new DirectoryInfo(sourceDirName);
            DirectoryInfo[] dirs = dir.GetDirectories();

            // Создание новой директории
            Directory.CreateDirectory(destDirName);

            // Получение списка файлов
            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destDirName, file.Name);
                file.CopyTo(tempPath, false);
            }

            // Рекурсия для копирования всех файлов
            foreach (DirectoryInfo subdir in dirs)
            {
                string tempPath = Path.Combine(destDirName, subdir.Name);
                DirectoryCopy(subdir.FullName, tempPath);
            }
        }

        // Функция вычисления размера каталога
        private static long DirSize(DirectoryInfo d)
        {

            try
            {
                long size = 0;
                // Добавление размера файлов в текущей директории
                FileInfo[] fis = d.GetFiles();
                foreach (FileInfo fi in fis)
                {
                    size += fi.Length;
                }
                // Добавление размера дочерних директорий
                DirectoryInfo[] dis = d.GetDirectories();
                foreach (DirectoryInfo di in dis)
                {
                    size += DirSize(di);
                }
                return size;
            }
            catch (Exception e)
            {
                WriteError($"{DateTime.Now.ToString()}: {e.Message}");
                return 0;
            } 
        }

        // Функция для вывода окна информации
        private static void PrintInfo(string Field, string Info)
        {
            string fullInfo = $"{Field}: {Info}";
            bool bPath = false;
            for (int j = 0; j < 125; j++)
            {
                if (j == 0 || j == 124)
                {
                    Console.Write("|");
                }
                else if (bPath == false)
                {
                    Console.Write(fullInfo);
                    j = fullInfo.Length;
                    bPath = true;
                }
                else Console.Write(" ");
            }
            Console.WriteLine();
        }

        // Фукция вывода директории
        private static void PrintDirectory(StringBuilder Info)
        {
            while (!Info.Equals(""))
            {
                bool bPath = false;
                for (int j = 0; j < 125; j++)
                {
                    if (j == 0 || j == 124)
                    {
                        Console.Write("|");
                    }
                    else if (Info.Length > 123)
                    {
                        Console.Write(Info.ToString(0, 123));
                        j = 123;
                        Info.Remove(0, 123);
                    }
                    else if (Info.Length < 123 && bPath == false)
                    {
                        Console.Write(Info.ToString(0, Info.Length));
                        j = Info.Length;
                        Info.Clear();
                        bPath = true;
                    }
                    else Console.Write(" ");
                }
                Console.WriteLine();
            }
        }

        // Функция записи ошибок/исключений в файл
        private static void WriteError(string ErrorText)
        {
            string path = Path.Combine(errDir, "errors.txt");
            if (File.Exists(path) == false) File.AppendAllText(path, ErrorText);
            else File.AppendAllText(path, Environment.NewLine + ErrorText);
        }
    }

    // Data-класс для создания целевого объекта
    class Data
    {
        public string name;
        public string creationTime;
        public string lastWriteTime;
        public string size;
        public string path;
    }
}