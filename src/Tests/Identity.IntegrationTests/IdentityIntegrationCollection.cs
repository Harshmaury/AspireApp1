using Identity.IntegrationTests.Fixtures;
using Xunit;

namespace Identity.IntegrationTests;

[CollectionDefinition("IdentityIntegration")]
public sealed class IdentityIntegrationCollection : ICollectionFixture<IdentityIntegrationFixture> { }
