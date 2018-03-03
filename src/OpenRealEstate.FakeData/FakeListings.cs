﻿using FizzWare.NBuilder;
using FizzWare.NBuilder.Generators;
using OpenRealEstate.Core;
using OpenRealEstate.Core.Land;
using OpenRealEstate.Core.Rental;
using OpenRealEstate.Core.Residential;
using OpenRealEstate.Core.Rural;
using System;
using System.Collections.Generic;
using System.Linq;
using CategoryType = OpenRealEstate.Core.Land.CategoryType;

namespace OpenRealEstate.FakeData
{
    public class FakeListings
    {
        private static Random _random = new Random(Guid.NewGuid().GetHashCode());

        public static T CreateAFakeListing<T>() where T : Listing, new()
        {
            // NBuilder fails to return the last enum type, here :(
            // var statusType = GetRandom.Enumeration<StatusType>();

            var values = EnumHelper.GetValues(typeof(StatusType));
            var randomIndex = _random.Next(0, values.Length);
            var statusType =  (StatusType)values.GetValue(randomIndex);
            if (statusType == StatusType.Unknown)
            {
                statusType = StatusType.Available;
            }

            var createdOn = GetRandom.DateTime(DateTime.UtcNow.AddDays(-7), DateTime.UtcNow.AddDays(-1));
            createdOn = DateTime.SpecifyKind(createdOn, DateTimeKind.Utc);
            
            var updatedOn = GetRandom.DateTimeFrom(DateTime.UtcNow.AddDays(-1));
            updatedOn = DateTime.SpecifyKind(updatedOn, DateTimeKind.Utc);
            
            // Yep, copied from FakeCommonListingHelpers because that is method is (correctly) internal :/
            var listing = Builder<T>.CreateNew()
                                    .With(x => x.Id, $"listing-{GetRandom.Int()}")
                                    .With(x => x.AgencyId, $"Agency-{GetRandom.String(6)}")
                                    .With(x => x.StatusType, statusType) // NOTE: SourceStatus is defined AFTER this instance is created.
                                    .With(x => x.Address, FakeAddress.CreateAFakeAddress())
                                    .With(x => x.Agents, FakeAgent.CreateFakeAgents())
                                    .With(x => x.LandDetails, CreateLandDetails())
                                    .With(x => x.Features, CreateFeatures())
                                    .With(x => x.CreatedOn, createdOn)
                                    .With(x => x.UpdatedOn, updatedOn)
                                    .With(x => x.Images, CreateMedia(GetRandom.Int(1, 21)))
                                    .With(x => x.FloorPlans, CreateMedia(GetRandom.Int(0, 3)))
                                    .With(x => x.Documents, CreateMedia(GetRandom.Int(0, 4)))
                                    .With(x => x.Inspections, CreateInspections(GetRandom.Int(0, 4)))
                                    .Do(x =>
                                    {
                                        if (x is ResidentialListing residentialListing)
                                        {
                                            // Skip first enumeration -> Unknown.
                                            var index = GetRandom.Int(1, Enum.GetValues(typeof(PropertyType)).Length);
                                            residentialListing.PropertyType = (PropertyType)Enum.GetValues(typeof(PropertyType)).GetValue(index);
                                            residentialListing.AuctionOn = DateTime.UtcNow;
                                            residentialListing.BuildingDetails = CreateBuildingDetails();
                                            residentialListing.Pricing = Builder<SalePricing>.CreateNew().Build();
                                            residentialListing.Pricing = CreateSalePricing(residentialListing.StatusType);
                                        }
                                        else if (x is RentalListing rentalListing)
                                        {
                                            var index = GetRandom.Int(1, Enum.GetValues(typeof(PropertyType)).Length);
                                            rentalListing.PropertyType = (PropertyType)Enum.GetValues(typeof(PropertyType)).GetValue(index);
                                            rentalListing.AvailableOn = DateTime.UtcNow;
                                            rentalListing.BuildingDetails = CreateBuildingDetails();
                                            rentalListing.Pricing = Builder<RentalPricing>.CreateNew()
                                                                                          .With(y => y.PaymentFrequencyType, PaymentFrequencyType.Weekly)
                                                                                          .Build();
                                            if (rentalListing.StatusType == StatusType.Available)
                                            {
                                                rentalListing.Pricing.RentedOn = null;
                                            }
                                        }
                                        else if (x is LandListing landListing)
                                        {
                                            var index = GetRandom.Int(1, Enum.GetValues(typeof(CategoryType)).Length);
                                            landListing.CategoryType = (CategoryType)Enum.GetValues(typeof(CategoryType)).GetValue(index);
                                            landListing.AuctionOn = DateTime.UtcNow;
                                            landListing.Estate = Builder<LandEstate>.CreateNew().Build();
                                            landListing.Pricing = CreateSalePricing(landListing.StatusType);
                                        }
                                        else if (x is RuralListing ruralListing)
                                        {
                                            var index = GetRandom.Int(1, Enum.GetValues(typeof(Core.Rural.CategoryType)).Length);
                                            ruralListing.CategoryType = (Core.Rural.CategoryType)Enum.GetValues(typeof(Core.Rural.CategoryType)).GetValue(index);
                                            ruralListing.AuctionOn = DateTime.UtcNow;
                                            ruralListing.Pricing = CreateSalePricing(ruralListing.StatusType);
                                            ruralListing.RuralFeatures = Builder<RuralFeatures>.CreateNew().Build();
                                            ruralListing.BuildingDetails = CreateBuildingDetails();
                                        }

                                    }).Build();

            FakeCommonListingHelpers.SetSourceStatus(listing);

            return listing;
        }

        public static IList<T> CreateFakeListings<T>(int numberOfFakeListings = 20) where T : Listing, new()
        {
            if (numberOfFakeListings <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(numberOfFakeListings),
                    "You need at least ONE fake listing to be requested.");
            }

            var listings = new List<T> {CreateFixedListing<T>()};

            // Start at 1 because the first listing should be the hard-coded one.
            for (var i = 1; i < numberOfFakeListings; i++)
            {
                listings.Add(CreateAFakeListing<T>());
            }

            return listings;
        }

        public static ResidentialListing CreateAFakeResidentialListing(string id = "Residential-Current-ABCD1234",
                                                                       PropertyType propertyType = PropertyType.House)
        {
            var listing = new ResidentialListing
            {
                Id = id
            };

            FakeCommonListingHelpers.SetCommonListingData(listing);
            FakeCommonListingHelpers.SetBuildingDetails(listing);
            FakeCommonListingHelpers.SetSalePrice(listing);

            listing.PropertyType = propertyType;
            listing.AuctionOn = new DateTime(2009, 2, 4, 18, 30, 0, DateTimeKind.Utc);
            listing.CouncilRates = "$2000 per month";

            return listing;
        }

        public static RentalListing CreateAFakeRentalListing(string id = "Rental-Current-ABCD1234",
                                                             PropertyType propertyType = PropertyType.House)
        {
            var listing = new RentalListing()
            {
                Id = id
            };

            FakeCommonListingHelpers.SetCommonListingData(listing);
            listing.Features.Tags.Remove("houseAndLandPackage");

            FakeCommonListingHelpers.SetBuildingDetails(listing);
            SetRentalPricing(listing);

            listing.AvailableOn = new DateTime(2009, 1, 26, 12, 30, 00, DateTimeKind.Utc);
            listing.PropertyType = propertyType;

            return listing;
        }

        public static LandListing CreateAFakeLandListing(string id = "Land-Current-ABCD1234",
                                                         PropertyType propertyType = PropertyType.House)
        {
            var listing = new LandListing
            {
                Id = id
            };

            FakeCommonListingHelpers.SetCommonListingData(listing);
            listing.Address.StreetNumber = "LOT 12/39";
            listing.Features.Bedrooms = 0;
            listing.Features.Bathrooms = 0;
            listing.Features.Ensuites = 0;
            listing.Features.CarParking = new CarParking();

            FakeCommonListingHelpers.SetSalePrice(listing);
            SetLandEstate(listing);
            listing.AuctionOn = new DateTime(2009, 1, 24, 12, 30, 00, DateTimeKind.Utc);
            listing.CategoryType = CategoryType.Residential;
            listing.CouncilRates = "$2000 per month";

            listing.Features.Tags.Remove("houseAndLandPackage");
            listing.Features.Tags.Remove("isANewConstruction");
            listing.Features.Tags.Remove("hotWaterService-gas");
            listing.Features.Tags.Remove("heating-other");
            listing.Features.Tags.Remove("balcony");
            listing.Features.Tags.Remove("shed");
            listing.Features.Tags.Remove("courtyard");

            return listing;
        }

        public static RuralListing CreateAFakeRuralListing(string id = "Rural-Current-ABCD1234",
                                                           PropertyType propertyType = PropertyType.House)
        {
            var listing = new RuralListing()
            {
                Id = id
            };

            FakeCommonListingHelpers.SetCommonListingData(listing);
            listing.Features.Tags.Remove("houseAndLandPackage");
            listing.Features.Tags.Remove("isANewConstruction");

            FakeCommonListingHelpers.SetBuildingDetails(listing);
            FakeCommonListingHelpers.SetSalePrice(listing);

            SetRuralFeatures(listing);

            listing.AuctionOn = new DateTime(2009, 1, 24, 14, 30, 00, DateTimeKind.Utc);
            listing.CategoryType = Core.Rural.CategoryType.Cropping;
            listing.CouncilRates = "$2,200 per annum";

            return listing;
        }

        private static T CreateFixedListing<T>() where T : Listing, new()
        {
            if (typeof(T) == typeof(ResidentialListing))
            {
                return CreateAFakeResidentialListing() as T;
            }

            if (typeof(T) == typeof(RentalListing))
            {
                return CreateAFakeRentalListing() as T;
            }

            if (typeof(T) == typeof(LandListing))
            {
                return CreateAFakeLandListing() as T;
            }

            if (typeof(T) == typeof(RuralListing))
            {
                return CreateAFakeRuralListing() as T;
            }

            throw new Exception($"The type '{typeof(T)}' was not handled.");
        }

        private static void SetRentalPricing(RentalListing listing)
        {
            listing.Pricing = new RentalPricing
            {
                PaymentFrequencyType = PaymentFrequencyType.Weekly,
                RentalPrice = 350,
                RentalPriceText = "$350",
                Bond = 999
            };
        }

        private static void SetLandEstate(LandListing listing)
        {
            listing.Estate = new LandEstate
            {
                Name = "Panorama",
                Stage = "5"
            };
        }

        private static void SetRuralFeatures(RuralListing listing)
        {
            listing.RuralFeatures = new RuralFeatures
            {
                AnnualRainfall = "250 mm per annum",
                CarryingCapacity = "400 Deer or 100 head of breeding Cattle",
                Fencing = "Boundary and internal fencing all in good condition",
                Improvements = "Shearing shed, barn and machinery shed.",
                Irrigation = "Electric pump from dam and bore.",
                Services = "Power, telephone, airstrip, school bus, mail."
            };
        }

        private static LandDetails CreateLandDetails()
        {
            return Builder<LandDetails>.CreateNew()
                                       .With(x => x.Area, Builder<UnitOfMeasure>.CreateNew().Build())
                                       .With(x => x.Frontage, Builder<UnitOfMeasure>.CreateNew().Build())
                                       .With(x => x.Depths, Builder<Depth>.CreateListOfSize(1).Build())
                                       .Build();
        }

        private static Features CreateFeatures()
        {
            return Builder<Features>.CreateNew()
                                    .With(x => x.CarParking, Builder<CarParking>.CreateNew().Build())
                                    .Build();
        }

        private static IEnumerable<Media> CreateMedia(int listSize,
                                                      string contentType = "image/jpeg")
        {
            if (listSize <= 0)
            {
                return Enumerable.Empty<Media>();
            }

            return Builder<Media>.CreateListOfSize(listSize)
                .All()
                .With(x => x.ContentType, contentType)
                .With(x => x.CreatedOn, DateTime.UtcNow)
                .With(x => x.Url = GetRandom.Url()) // Each one needs to be unique, not reusing the same value. Notice the equals (=) vs the comma (,) ways.
                .Build();
        }

        private static IEnumerable<Inspection> CreateInspections(int listSize)
        {
            if (listSize <= 0)
            {
                return Enumerable.Empty<Inspection>();
            }

            // NOTE: yes yes .. the _random_ closed time might be BEFORE the open time :/
            //       I'm just not sure how to handle this in a list to make sure closed > open.
            return Builder<Inspection>.CreateListOfSize(listSize)
                .All()
                .With(x => x.OpensOn = GetRandom.DateTime(DateTime.UtcNow.AddHours(40), DateTime.UtcNow.AddHours(50)))
                .With(x => x.ClosesOn= GetRandom.DateTime(DateTime.UtcNow.AddHours(40), DateTime.UtcNow.AddHours(50)))
                .Build();
        }

        private static BuildingDetails CreateBuildingDetails()
        {
            return Builder<BuildingDetails>.CreateNew()
                                           .With(x => x.Area, Builder<UnitOfMeasure>.CreateNew().Build())
                                           .Build();
        }

        private static SalePricing CreateSalePricing(StatusType statusType)
        {
            var salePricing = Builder<SalePricing>.CreateNew().Build();
            if (statusType == StatusType.Available)
            {
                salePricing.SoldOn = null;
                salePricing.SoldPrice = null;
                salePricing.SoldPriceText = null;
            }

            return salePricing;
        }
    }
}