using Kafka.IntegrationTests.Fixtures;

namespace Kafka.IntegrationTests;

[CollectionDefinition("KafkaPostgres")]
public sealed class KafkaPostgresCollection : ICollectionFixture<KafkaPostgresFixture> { }
