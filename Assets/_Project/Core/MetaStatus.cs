namespace Game.Core
{
    public enum MetaStatus
    {
        Available = 0,
        NotEnoughCurrency = 1,
        Maxed = 2,
        NeedsParents = 3,
        Excluded = 4,
        GateUnmet = 5,
        NotForged = 6,
        NotOwned = 7,
        SocketLocked = 8,
        WrongColor = 9,
        Unavailable = 10,
        Invalid = 11
    }

    public readonly struct MetaOffer
    {
        public MetaOffer(MetaStatus status, int cost)
        {
            Status = status;
            Cost = cost;
        }

        public MetaStatus Status { get; }
        public int Cost { get; }
        public bool CanBuy => Status == MetaStatus.Available;
    }
}
