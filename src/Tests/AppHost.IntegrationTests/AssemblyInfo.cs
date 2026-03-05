using Xunit;

// TST-9 fix: removed assembly-level DisableTestParallelization.
// Each test suite is already isolated by named [Collection] attributes
// (ExaminationSuite, FeeSuite, StudentSuite) which use ICollectionFixture
// to scope the Postgres container per collection. That is the correct
// isolation mechanism — the global override was an unnecessary blunt hammer
// that serialised the entire assembly and added significant CI runtime.
