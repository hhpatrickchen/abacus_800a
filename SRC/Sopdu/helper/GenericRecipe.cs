using System;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Xml.Serialization;

namespace Sopdu.helper
{
    public class GenericRecipe<T>
    {
        private const int MAX_RECIPE_FILES = 256;

        // The archive is designed to keep the recipe changes for troubleshooting purpose.
        // Recipe files are named by it's version number.
        private ZipArchive recipeArchive;

        private XmlSerializer recipeSerializer;

        public GenericRecipe(string filePath)
        {
            this.FilePath = filePath;
            recipeSerializer = new XmlSerializer(typeof(T));
        }

        public string FilePath { get; private set; }

        public void Create(T recipe)
        {
            recipeArchive = ZipFile.Open(FilePath, ZipArchiveMode.Create);
            int latestRecipeNumber = 1;//must be zero for new list
            ZipArchiveEntry recipeEntry = recipeArchive.CreateEntry(latestRecipeNumber + ".xml", CompressionLevel.Fastest);
            recipeSerializer.Serialize(recipeEntry.Open(), recipe);
            recipeArchive.Dispose();
        }

        public T Read()
        {
            recipeArchive = ZipFile.Open(FilePath, ZipArchiveMode.Read);
            ZipArchiveEntry latestRecipe = null;
            int latestRecipeNumber = 0;
            for (int i = 0; i < recipeArchive.Entries.Count; i++)
            {
                int recipeNumber;
                string[] entryName = recipeArchive.Entries[i].Name.Split('.');
                if (entryName[1].Equals("xml") && entryName.Length == 2)
                {
                    if (int.TryParse(entryName[0], out recipeNumber))
                    {
                        if (recipeNumber > latestRecipeNumber)
                        {
                            latestRecipeNumber = recipeNumber;
                            latestRecipe = recipeArchive.Entries[i];
                        }
                    }
                }
            }
            if (latestRecipe == null)
            {
                throw new Exception("No Recipe Found.");
            }
            object recipe = recipeSerializer.Deserialize(latestRecipe.Open());
            recipeArchive.Dispose();
            return (T)recipe;
        }

        public void Write(T recipe)
        {
            recipeArchive = ZipFile.Open(FilePath, ZipArchiveMode.Update);
            int latestRecipeNumber = 0;
            int oldestRecipeNumber = int.MaxValue;
            ZipArchiveEntry oldestRecipe = null;
            for (int i = 0; i < recipeArchive.Entries.Count; i++)
            {
                int recipeNumber;
                string[] entryName = recipeArchive.Entries[i].Name.Split('.');
                if (entryName[1].Equals("xml") && entryName.Length == 2)
                {
                    if (int.TryParse(entryName[0], out recipeNumber))
                    {
                        if (recipeNumber >= latestRecipeNumber)
                        {
                            latestRecipeNumber = recipeNumber;
                        }
                        if (recipeNumber < oldestRecipeNumber)
                        {
                            oldestRecipeNumber = recipeNumber;
                            oldestRecipe = recipeArchive.Entries[i];
                        }
                    }
                }
            }
            if ((oldestRecipe != null) && (recipeArchive.Entries.Count > MAX_RECIPE_FILES))
            {
                oldestRecipe.Delete();// some logical error here
            }
            latestRecipeNumber = latestRecipeNumber + 1;
            ZipArchiveEntry recipeEntry = recipeArchive.CreateEntry(latestRecipeNumber + ".xml", CompressionLevel.Fastest);
            recipeSerializer.Serialize(recipeEntry.Open(), recipe);
            recipeArchive.Dispose();
        }
    }

    public class MicronTo_mmConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            double displayvalue = (double)((double)((long)value) / 100);

            return displayvalue;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    public class Micron2To_mmConvertor : IValueConverter
    {
        public object Convert(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            double displayvalue = (double)((double)value / 100);

            return displayvalue;
        }

        public object ConvertBack(object value, Type targetType,
            object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}