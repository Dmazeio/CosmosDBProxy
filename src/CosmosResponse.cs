#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
public class CosmosResponse
{
    public string _self { get; set; }
    public string id { get; set; }
    public string _rid { get; set; }
    public string media { get; set; }
    public string addresses { get; set; }
    public string _dbs { get; set; }
    public Writablelocation[] writableLocations { get; set; }
    public Readablelocation[] readableLocations { get; set; }
    public bool enableMultipleWriteLocations { get; set; }
    public Userreplicationpolicy userReplicationPolicy { get; set; }
    public Userconsistencypolicy userConsistencyPolicy { get; set; }
    public Systemreplicationpolicy systemReplicationPolicy { get; set; }
    public Readpolicy readPolicy { get; set; }
    public string queryEngineConfiguration { get; set; }
}


public class Userreplicationpolicy
{
    public bool asyncReplication { get; set; }
    public int minReplicaSetSize { get; set; }
    public int maxReplicasetSize { get; set; }
}

public class Userconsistencypolicy
{
    public string defaultConsistencyLevel { get; set; }
}

public class Systemreplicationpolicy
{
    public int minReplicaSetSize { get; set; }
    public int maxReplicasetSize { get; set; }
}

public class Readpolicy
{
    public int primaryReadCoefficient { get; set; }
    public int secondaryReadCoefficient { get; set; }
}

public class Writablelocation
{
    public string name { get; set; }
    public string databaseAccountEndpoint { get; set; }
}

public class Readablelocation
{

    public string name { get; set; }
    public string databaseAccountEndpoint { get; set; }
}

#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
