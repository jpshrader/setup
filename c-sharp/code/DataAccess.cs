using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;

namespace project {
	public abstract class DataAccess {
		private readonly string connectionString;

		internal DataAccess(string connectionString) {
			this.connectionString = connectionString; 
		}

		protected static IEnumerable<SqlParameter> GetSqlParameters(params SqlParameter[] sqlParameters) => sqlParameters;

		protected async Task<int> ExecuteNonQuery(string procName, IEnumerable<SqlParameter> sqlParameters = null) {
			return await ExecuteQuery(procName, (command) => command.ExecuteNonQueryAsync(), sqlParameters);
		}

		protected async Task<T> Fetch<T>(string procName, Func<SqlDataReader, T> mapper, IEnumerable<SqlParameter> sqlParameters = null, CommandType commandType = CommandType.Text) {
			async Task<T> getItem(SqlCommand command) {
				var reader = await command.ExecuteReaderAsync();

				return mapper(reader);
			}

			return await ExecuteQuery(procName, getItem, sqlParameters, commandType);
		}

		protected async Task<IEnumerable<T>> Get<T>(string sql, Func<SqlDataReader, T> mapper, IEnumerable<SqlParameter> sqlParameters = null, CommandType commandType = CommandType.Text) {
			async Task<IEnumerable<T>> getList(SqlCommand command) {
				var reader = await command.ExecuteReaderAsync();

				var objects = new List<T>();
				while (reader.Read()) {
					objects.Add(mapper(reader));
				}

				return objects;
			}

			return await ExecuteQuery(sql, getList, sqlParameters, commandType);
		}

		private async Task<T> ExecuteQuery<T>(string sql, Func<SqlCommand, Task<T>> func, IEnumerable<SqlParameter> sqlParameters = null, CommandType commandType = CommandType.Text) {
			using var sqlConnection = new SqlConnection(connectionString);
			using var sqlCommand = new SqlCommand(sql, sqlConnection) {
				CommandType = commandType
			};
			if (sqlParameters != null) {
				sqlCommand.Parameters.AddRange(sqlParameters.ToArray());
			}
			await sqlConnection.OpenAsync();

			return await func(sqlCommand);
		}
	}
}
