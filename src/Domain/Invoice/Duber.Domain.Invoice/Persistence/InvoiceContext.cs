using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Dapper;
using Duber.Domain.SharedKernel.Chaos;
using Duber.Infrastructure.Chaos;
using Duber.Infrastructure.DDD;
using Duber.Infrastructure.Extensions;
using Duber.Infrastructure.Resilience.Abstractions;
using MediatR;
using Polly;

namespace Duber.Domain.Invoice.Persistence
{
    public class InvoiceContext : IInvoiceContext
    {
        private Context _context;
        private IDbConnection _connection;
        private readonly IMediator _mediator;
        private readonly string _connectionString;
        private readonly GeneralChaosSetting _generalChaosSetting;
        private readonly IPolicyAsyncExecutor _resilientSqlExecutor;

        public InvoiceContext(string connectionString, IMediator mediator, IPolicyAsyncExecutor resilientSqlExecutor, GeneralChaosSetting generalChaosSetting)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException(nameof(connectionString));

            _connectionString = connectionString;
            _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
            _generalChaosSetting = generalChaosSetting ?? throw new ArgumentException(nameof(generalChaosSetting));
            _resilientSqlExecutor = resilientSqlExecutor ?? throw new ArgumentNullException(nameof(resilientSqlExecutor));

            // this gonna inject the same chaos monkeys for all sql operations inside of this repo.
            _context = new Context(OperationKeys.InvoiceDbOperations.ToString()).WithChaosSettings(_generalChaosSetting);
        }

        public async Task<int> ExecuteAsync<T>(T entity, string sql, object parameters = null, int? timeOut = null, CommandType? commandType = null)
            where T : Entity, IAggregateRoot
        {
            _connection = GetOpenConnection();
            var result = await _resilientSqlExecutor.ExecuteAsync(async (ctx) => await _connection.ExecuteAsync(sql, parameters, null, timeOut, commandType), _context);

            // ensures that all events are dispatched after the entity is saved successfully.
            await _mediator.DispatchDomainEventsAsync(entity);
            return result;
        }

        public async Task<IEnumerable<T>> QueryAsync<T>(string sql, object parameters = null, int? timeOut = null, CommandType? commandType = null)
            where T : Entity, IAggregateRoot
        {
            _connection = GetOpenConnection();
            return await _resilientSqlExecutor.ExecuteAsync(async (ctx) => await _connection.QueryAsync<T>(sql, parameters, null, timeOut, commandType), _context);
        }

        public async Task<T> QuerySingleAsync<T>(string sql, object parameters = null, int? timeOut = null, CommandType? commandType = null) where T : Entity, IAggregateRoot
        {
            _connection = GetOpenConnection();
            return await _resilientSqlExecutor.ExecuteAsync(async (ctx) => await _connection.QuerySingleOrDefaultAsync<T>(sql, parameters, null, timeOut, commandType), _context);
        }

        private IDbConnection GetOpenConnection()
        {
            if (_connection == null)
            {
                return new SqlConnection(_connectionString);
            }

            if (_connection.State == ConnectionState.Closed)
                _connection.Open();

            return _connection;
        }

        public void Dispose()
        {
            _connection?.Dispose();
        }
    }
}
