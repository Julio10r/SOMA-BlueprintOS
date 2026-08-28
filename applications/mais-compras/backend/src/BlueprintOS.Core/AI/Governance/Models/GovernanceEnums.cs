#pragma warning disable CS1591

namespace BlueprintOS.Core.AI.Governance.Models;

public enum GovernanceEnvironment
{
    Unknown = 0,
    Development = 1,
    Homologation = 2,
    Production = 3,
}

public enum ActionResourceType
{
    Unknown = 0,
    DatabaseTable = 1,
    DatabaseSchema = 2,
    ApiEndpoint = 3,
    FileExport = 4,
    Prompt = 5,
    Log = 6,
    Permission = 7,
    ExternalSystem = 8,

    /// <summary>A governance policy record that is itself governed (e.g. a WriteVerificationProfile
    /// version). Changing one is an ActionProposal, never a direct store write.</summary>
    GovernancePolicy = 9,
}

public enum ActionOperation
{
    Unknown = 0,
    Select = 1,
    SchemaDiscovery = 2,
    MetadataRead = 3,
    Analyze = 4,
    Compare = 5,
    Export = 6,
    Insert = 7,
    Update = 8,
    Delete = 9,
    Truncate = 10,
    Drop = 11,
    Alter = 12,
    Merge = 13,
    ExecuteProcedure = 14,
    Grant = 15,
    Revoke = 16,
    PersistSecret = 17,
    LogSecret = 18,
    PromptWithSecret = 19,

    /// <summary>Creation of a new governed record/version (e.g. a new WriteVerificationProfile version).
    /// Distinct from <see cref="Insert"/>, which means a physical row insert into a business table.</summary>
    Create = 20,
}

public enum DataClassification
{
    Unknown = 0,
    Public = 1,
    Internal = 2,
    Confidential = 3,
    PersonalData = 4,
    SensitivePersonalData = 5,
    SecretCredential = 6,
}

public enum ActionReversibility
{
    Unknown = 0,
    Reversible = 1,
    PartiallyReversible = 2,
    Irreversible = 3,
}

public enum RiskClassification
{
    Green = 1,
    Yellow = 2,
    Red = 3,
}

public enum PolicyDecisionStatus
{
    Allowed = 1,
    RequiresApproval = 2,
    Blocked = 3,
}

public enum ApprovalRequestStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Expired = 4,
    Revoked = 5,
}

