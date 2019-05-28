namespace Duber.Domain.SharedKernel.Chaos
{
    public enum OperationKeys
    {
        PaymentApi = 0,
        TripApiGet = 1,
        TripApiCreate = 2,
        TripApiAccept = 3,
        TripApiStart = 4,
        TripApiCancel = 5,
        TripApiUpdateCurrentLocation = 6,
        InvoiceDbOperations = 7,
        ReportingDbAddTrip = 8,
        ReportingDbUpdateTrip = 9,
        ReportingDbGetTrip = 10,
        ReportingDbGetTrips = 11,
        ReportingDbGetTripsByUser = 12,
        ReportingDbGetTripsByDriver = 13
    }
}
