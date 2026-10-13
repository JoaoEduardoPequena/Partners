using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Partners.Persistence.Extensions
{
    public static class DbUpdateExceptionExtensions
    {
        // 2601: linha duplicada num índice único; 2627: violação de UNIQUE/PRIMARY KEY constraint.
        public static bool IsUniqueConstraintViolation(this DbUpdateException exception) =>
            exception.InnerException is SqlException { Number: 2601 or 2627 };
    }
}
