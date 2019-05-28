using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Duber.Domain.SharedKernel.Chaos;
using Duber.Infrastructure.Chaos;
using Duber.Infrastructure.Resilience.Abstractions;
using Duber.WebSite.Infrastructure.Persistence;
using Duber.WebSite.Models;
using Microsoft.EntityFrameworkCore;
using Polly;

namespace Duber.WebSite.Infrastructure.Repository
{
    public class ReportingRepository : IReportingRepository
    {
        private readonly ReportingContext _reportingContext;
        private readonly IPolicyAsyncExecutor _resilientAsyncSqlExecutor;
        private readonly IPolicySyncExecutor _resilientSyncSqlExecutor;
        private readonly GeneralChaosSetting _generalChaosSetting;

        public ReportingRepository(ReportingContext reportingContext, IPolicyAsyncExecutor resilientAsyncSqlExecutor, IPolicySyncExecutor resilientSyncSqlExecutor, GeneralChaosSetting generalChaosSetting)
        {
            _reportingContext = reportingContext ?? throw new ArgumentNullException(nameof(reportingContext));
            _resilientAsyncSqlExecutor = resilientAsyncSqlExecutor ?? throw new ArgumentNullException(nameof(resilientAsyncSqlExecutor));
            _resilientSyncSqlExecutor = resilientSyncSqlExecutor ?? throw new ArgumentNullException(nameof(resilientSyncSqlExecutor));
            _generalChaosSetting = generalChaosSetting ?? throw new ArgumentException(nameof(generalChaosSetting));
        }

        public async Task AddTripAsync(Trip trip)
        {
            _reportingContext.Trips.Add(trip);
            var context = new Context(OperationKeys.ReportingDbAddTrip.ToString()).WithChaosSettings(_generalChaosSetting);
            await _resilientAsyncSqlExecutor.ExecuteAsync(async (ctx) => await _reportingContext.SaveChangesAsync(), context);
        }

        public void AddTrip(Trip trip)
        {
            _reportingContext.Trips.Add(trip);
            var context = new Context(OperationKeys.ReportingDbAddTrip.ToString()).WithChaosSettings(_generalChaosSetting);
            _resilientSyncSqlExecutor.Execute((ctx) => _reportingContext.SaveChanges(), context);
        }

        public void UpdateTrip(Trip trip)
        {
            _reportingContext.Attach(trip);
            var context = new Context(OperationKeys.ReportingDbUpdateTrip.ToString()).WithChaosSettings(_generalChaosSetting);
            _resilientSyncSqlExecutor.Execute((ctx) => _reportingContext.SaveChanges(), context);
        }

        public async Task UpdateTripAsync(Trip trip)
        {
            _reportingContext.Attach(trip);
            var context = new Context(OperationKeys.ReportingDbUpdateTrip.ToString()).WithChaosSettings(_generalChaosSetting);
            await _resilientAsyncSqlExecutor.ExecuteAsync(async (ctx) => await _reportingContext.SaveChangesAsync(), context);
        }

        public async Task<IList<Trip>> GetTripsAsync()
        {
            var context = new Context(OperationKeys.ReportingDbGetTrips.ToString()).WithChaosSettings(_generalChaosSetting);
            return await _resilientAsyncSqlExecutor.ExecuteAsync(async (ctx) => await _reportingContext.Trips.ToListAsync(), context);
        }

        public async Task<Trip> GetTripAsync(Guid tripId)
        {
            var context = new Context(OperationKeys.ReportingDbGetTrip.ToString()).WithChaosSettings(_generalChaosSetting);
            return await _resilientAsyncSqlExecutor.ExecuteAsync(async (ctx) =>
                await _reportingContext.Trips.SingleOrDefaultAsync(x => x.Id == tripId), context);
        }

        public Trip GetTrip(Guid tripId)
        {
            var context = new Context(OperationKeys.ReportingDbGetTrip.ToString()).WithChaosSettings(_generalChaosSetting);
            return _resilientSyncSqlExecutor.Execute((ctx) => _reportingContext.Trips.SingleOrDefault(x => x.Id == tripId), context);
        }

        public async Task<IList<Trip>> GetTripsByUserAsync(int userId)
        {
            var context = new Context(OperationKeys.ReportingDbGetTripsByUser.ToString()).WithChaosSettings(_generalChaosSetting);
            return await _resilientAsyncSqlExecutor.ExecuteAsync(async (ctx) =>
                await _reportingContext.Trips
                    .Where(x => x.UserId == userId)
                    .OrderByDescending(x => x.Created)
                    .ToListAsync(),
                context);
        }

        public async Task<IList<Trip>> GetTripsByDriverAsync(int driverid)
        {
            var context = new Context(OperationKeys.ReportingDbGetTripsByDriver.ToString()).WithChaosSettings(_generalChaosSetting);
            return await _resilientAsyncSqlExecutor.ExecuteAsync(async (ctx) =>
                await _reportingContext.Trips
                    .Where(x => x.DriverId == driverid)
                    .OrderByDescending(x => x.Created)
                    .ToListAsync(),
                context);
        }

        public void Dispose()
        {
            _reportingContext?.Dispose();
        }
    }
}