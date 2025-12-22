using System.Data.SqlClient;

namespace NormalAccountProject.Models
{
    public static class SqlDataReaderExtensions
    {
        public static bool HasColumn(this SqlDataReader dr, string columnName)
        {
            for (int i = 0; i < dr.FieldCount; i++)
            {
                if (dr.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }
}
