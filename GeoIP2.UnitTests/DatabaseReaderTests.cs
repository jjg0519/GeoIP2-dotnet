﻿#region

using MaxMind.Db;
using MaxMind.GeoIP2.Exceptions;
using MaxMind.GeoIP2.Responses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Xunit;

#endregion

namespace MaxMind.GeoIP2.UnitTests
{
    public class DatabaseReaderTests
    {
        private readonly string _anonymousIpDatabaseFile;
        private readonly string _cityDatabaseFile;
        private readonly string _connectionTypeDatabaseFile;
        private readonly string _countryDatabaseFile;
        private readonly string _domainDatabaseFile;
        private readonly string _enterpriseDatabaseFile;
        private readonly string _ispDatabaseFile;

        public DatabaseReaderTests()
        {
            var databaseDir = Path.Combine(TestUtils.TestDirectory, "TestData", "MaxMind-DB", "test-data");

            _anonymousIpDatabaseFile = Path.Combine(databaseDir, "GeoIP2-Anonymous-IP-Test.mmdb");
            _cityDatabaseFile = Path.Combine(databaseDir, "GeoIP2-City-Test.mmdb");
            _connectionTypeDatabaseFile = Path.Combine(databaseDir, "GeoIP2-Connection-Type-Test.mmdb");
            _countryDatabaseFile = Path.Combine(databaseDir, "GeoIP2-Country-Test.mmdb");
            _domainDatabaseFile = Path.Combine(databaseDir, "GeoIP2-Domain-Test.mmdb");
            _enterpriseDatabaseFile = Path.Combine(databaseDir, "GeoIP2-Enterprise-Test.mmdb");
            _ispDatabaseFile = Path.Combine(databaseDir, "GeoIP2-ISP-Test.mmdb");
        }

        [Fact]
        public void DatabaseReader_HasDatabaseMetadata()
        {
            using (var reader = new DatabaseReader(_domainDatabaseFile))
            {
                Assert.Equal("GeoIP2-Domain", reader.Metadata.DatabaseType);
            }
        }

        [Fact]
        public void DatabaseReaderInMemoryMode_ValidResponse()
        {
            using (var reader = new DatabaseReader(_cityDatabaseFile, FileAccessMode.Memory))
            {
                var response = reader.City("81.2.69.160");
                Assert.Equal("London", response.City.Name);
            }
        }

        [Fact]
        public void DatabaseReaderWithStreamConstructor_ValidResponse()
        {
            using (var streamReader = File.OpenText(_cityDatabaseFile))
            {
                using (var reader = new DatabaseReader(streamReader.BaseStream))
                {
                    var response = reader.City("81.2.69.160");
                    Assert.Equal("London", response.City.Name);
                }
            }
        }

        [Fact]
        public void InvalidCountryMethodForCityDatabase_ExceptionThrown()
        {
            using (var reader = new DatabaseReader(_cityDatabaseFile))
            {
                var exception = Record.Exception(() => reader.Country("10.10.10.10"));
                Assert.NotNull(exception);
                Assert.Contains(exception.Message,
#if !NETCOREAPP1_0
                    "A GeoIP2-City database cannot be opened with the Country method");
#else
                    "A GeoIP2-City database cannot be opened with the given method");
#endif
                Assert.IsType<InvalidOperationException>(exception);
            }
        }

        [Fact]
        public void AnonymousIP_ValidResponse()
        {
            using (var reader = new DatabaseReader(_anonymousIpDatabaseFile))
            {
                var ipAddress = "1.2.0.1";
                var response = reader.AnonymousIP(ipAddress);
                Assert.True(response.IsAnonymous);
                Assert.True(response.IsAnonymousVpn);
                Assert.False(response.IsHostingProvider);
                Assert.False(response.IsPublicProxy);
                Assert.False(response.IsTorExitNode);
                Assert.Equal(ipAddress, response.IPAddress);
            }
        }

        [Fact]
        public void ConnectionType_ValidResponse()
        {
            using (var reader = new DatabaseReader(_connectionTypeDatabaseFile))
            {
                var ipAddress = "1.0.1.0";
                var response = reader.ConnectionType(ipAddress);
                Assert.Equal("Cable/DSL", response.ConnectionType);
                Assert.Equal(ipAddress, response.IPAddress);
            }
        }

        [Fact]
        public void Domain_ValidResponse()
        {
            using (var reader = new DatabaseReader(_domainDatabaseFile))
            {
                var ipAddress = "1.2.0.0";
                var response = reader.Domain(ipAddress);
                Assert.Equal("maxmind.com", response.Domain);
                Assert.Equal(ipAddress, response.IPAddress);
            }
        }

        [Fact]
        public void Enterprise_ValidResponse()
        {
            using (var reader = new DatabaseReader(_enterpriseDatabaseFile))
            {
                var ipAddress = "74.209.24.0";
                var response = reader.Enterprise(ipAddress);
                Assert.Equal(11, response.City.Confidence);
                Assert.Equal(99, response.Country.Confidence);
                Assert.Equal(6252001, response.Country.GeoNameId);
                Assert.Equal(27, response.Location.AccuracyRadius);
                Assert.Equal("Cable/DSL", response.Traits.ConnectionType);
                Assert.True(response.Traits.IsLegitimateProxy);
                Assert.Equal(ipAddress, response.Traits.IPAddress);
            }
        }

        [Fact]
        public void Isp_ValidResponse()
        {
            using (var reader = new DatabaseReader(_ispDatabaseFile))
            {
                var ipAddress = "1.128.0.0";
                var response = reader.Isp(ipAddress);
                Assert.Equal(1221, response.AutonomousSystemNumber);
                Assert.Equal("Telstra Pty Ltd", response.AutonomousSystemOrganization);
                Assert.Equal("Telstra Internet", response.Isp);
                Assert.Equal("Telstra Internet", response.Organization);
                Assert.Equal(ipAddress, response.IPAddress);
            }
        }

        [Fact]
        public void Country_ValidResponse()
        {
            using (var reader = new DatabaseReader(_countryDatabaseFile))
            {
                var response = reader.Country("81.2.69.160");
                Assert.Equal("GB", response.Country.IsoCode);
            }
        }

        [Fact]
        public void CountryWithIPAddressClass_ValidResponse()
        {
            using (var reader = new DatabaseReader(_countryDatabaseFile))
            {
                var response = reader.Country(IPAddress.Parse("81.2.69.160"));
                Assert.Equal("GB", response.Country.IsoCode);
            }
        }

        [Fact]
        public void City_ValidResponse()
        {
            using (var reader = new DatabaseReader(_cityDatabaseFile))
            {
                var response = reader.City("81.2.69.160");
                Assert.Equal("London", response.City.Name);
                Assert.Equal(100, response.Location.AccuracyRadius);
            }
        }

        [Fact]
        public void TryCity_ValidResponse()
        {
            using (var reader = new DatabaseReader(_cityDatabaseFile))
            {
                CityResponse response;
                var lookupSuccess = reader.TryCity("81.2.69.160", out response);
                Assert.True(lookupSuccess);
                Assert.Equal("London", response.City.Name);
            }
        }

        [Fact]
        public void City_ResponseHasIPAddress()
        {
            using (var reader = new DatabaseReader(_cityDatabaseFile))
            {
                var response = reader.City("81.2.69.160");
                Assert.Equal("81.2.69.160", response.Traits.IPAddress);
            }
        }

        [Fact]
        public void CityWithIPAddressClass_ValidResponse()
        {
            using (var reader = new DatabaseReader(_cityDatabaseFile))
            {
                var response = reader.City(IPAddress.Parse("81.2.69.160"));
                Assert.Equal("London", response.City.Name);
            }
        }

        [Fact]
        public void CityWithDefaultLocale_ValidResponse()
        {
            using (var reader = new DatabaseReader(_cityDatabaseFile))
            {
                var response = reader.City("81.2.69.160");
                Assert.Equal("London", response.City.Name);
            }
        }

        [Fact]
        public void CityWithLocaleList_ValidResponse()
        {
            using (
                var reader = new DatabaseReader(_cityDatabaseFile, new List<string> {"xx", "ru", "pt-BR", "es", "en"}))
            {
                var response = reader.City("81.2.69.160");
                Assert.Equal("Лондон", response.City.Name);
            }
        }

        [Fact]
        public void CityWithUnknownAddress_ExceptionThrown()
        {
            using (var reader = new DatabaseReader(_cityDatabaseFile))
            {
                var exception = Record.Exception(() => reader.City("10.10.10.10"));
                Assert.NotNull(exception);
                Assert.Contains("10.10.10.10 is not in the database", exception.Message);
                Assert.IsType<AddressNotFoundException>(exception);
            }
        }

        [Fact]
        public void TryCityUnknownAddress_False()
        {
            using (var reader = new DatabaseReader(_cityDatabaseFile))
            {
                CityResponse response;
                var status = reader.TryCity("10.10.10.10", out response);
                Assert.False(status);
                Assert.Null(response);
            }
        }
    }
}