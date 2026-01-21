 using Microsoft.EntityFrameworkCore;
using SnomiAssignmentReal.Helpers; // Make sure this matches your PasswordHelper namespace
using SnomiAssignmentReal.Models;

namespace SnomiAssignmentReal.Data
{
    public static class DbInitializer
    {
        public static void Seed(ApplicationDbContext context)
        {
            // Apply any pending migrations
            context.Database.Migrate();

            // 🔐 Seed Admin & Staff users
            if (!context.Users.Any())
            {
                var users = new List<User>
                {
                    new User
                    {
                        UserId = "U100",
                        UserFullName = "Manon Bannerman",
                        LoginEmailAddress = "manonadmin@snomi.com",
                        HashedPassword = PasswordHasher.HashPassword("Manon@123"), // ✅ Hashing here
                        UserRoleId = "UR100",
                        UserProfileImageUrl = "wwwroot/images/admin.jpg"
                    },
                    new User
                    {
                        UserId = "U200",
                        UserFullName = "Jennie Ruby Jane",
                        LoginEmailAddress = "jenniestaff@snomi.com",
                        HashedPassword = PasswordHasher.HashPassword("Jennie@123"),
                        UserRoleId = "UR101",
                        UserProfileImageUrl = "wwwroot/images/staff1.jpg"
                    },
                    new User
                    {
                        UserId = "U201",
                        UserFullName = "Roséanne Park",
                        LoginEmailAddress = "rosestaff@snomi.com",
                        HashedPassword = PasswordHasher.HashPassword("Rose@123"),
                        UserRoleId = "UR101",
                        UserProfileImageUrl = "wwwroot/images/staff2.jpg"
                    }
                };

                context.Users.AddRange(users);
                context.SaveChanges();
            }

            if (!context.MenuItems.Any())
            {
                var menuItems = new List<MenuItem>
                {
                    new MenuItem { MenuItemId = "M001", MenuItemName = "Avocado Toast", MenuItemDescription = "Thick multigrain toast topped with creamy avocado, cherry tomatoes, and a sprinkle of sesame seeds.", MenuItemCalories = 320, CategoryId = "C100", MenuItemUnitPrice = 14.90M, MenuItemImageUrl = "/images/menu/AvacadoToast.jpg" },
                    new MenuItem { MenuItemId = "M002", MenuItemName = "Scrambled Eggs with Sourdough", MenuItemDescription = "Fluffy scrambled eggs served on warm, buttered sourdough bread—a comforting classic.", MenuItemCalories = 400, CategoryId = "C100", MenuItemUnitPrice = 13.50M, MenuItemImageUrl = "/images/menu/ScrambledEggs.jpg" },
                    new MenuItem { MenuItemId = "M003", MenuItemName = "Big Breakfast", MenuItemDescription = "A hearty plate featuring eggs, sausages, toast, hash browns, sautéed mushrooms, and fresh greens.", MenuItemCalories = 750, CategoryId = "C100", MenuItemUnitPrice = 19.90M, MenuItemImageUrl = "/images/menu/BigBreakfast.jpg" },
                    new MenuItem { MenuItemId = "M004", MenuItemName = "Pancakes with fruits", MenuItemDescription = "Fluffy pancakes stacked high and topped with seasonal fruits and a drizzle of golden honey.", MenuItemCalories = 480, CategoryId = "C100", MenuItemUnitPrice = 14.90M, MenuItemImageUrl = "/images/menu/Pancake.jpg" },
                    new MenuItem { MenuItemId = "M005", MenuItemName = "Chicken Pesto Panini", MenuItemDescription = "Grilled chicken breast, basil pesto, tomatoes, and melted cheese pressed in warm panini bread.", MenuItemCalories = 530, CategoryId = "C100", MenuItemUnitPrice = 16.90M, MenuItemImageUrl = "/images/menu/ChickenPesto.jpg" },
                    new MenuItem { MenuItemId = "M006", MenuItemName = "Reuben Sandwich", MenuItemDescription = "A New York-style favorite with corned beef, tangy sauerkraut, Swiss cheese, and mustard on rye.", MenuItemCalories = 610, CategoryId = "C101", MenuItemUnitPrice = 15.90M, MenuItemImageUrl = "/images/menu/ReubenSandwich.jpg" },
                    new MenuItem { MenuItemId = "M007", MenuItemName = "Cheese and Mushroom Melt", MenuItemDescription = "Savory sautéed mushrooms paired with a rich, gooey three-cheese blend on toasted bread.", MenuItemCalories = 490, CategoryId = "C101", MenuItemUnitPrice = 12.50M, MenuItemImageUrl = "/images/menu/CheeseMushroom.jpg" },
                    new MenuItem { MenuItemId = "M008", MenuItemName = "Turkey Club Sandwich", MenuItemDescription = "Layered turkey breast, lettuce, tomato, hard-boiled egg, and mayo between toasted slices.", MenuItemCalories = 580, CategoryId = "C101", MenuItemUnitPrice = 14.90M, MenuItemImageUrl = "/images/menu/TurkeySandwich.jpg" },
                    new MenuItem { MenuItemId = "M009", MenuItemName = "Grilled Chicken Caesar Salad", MenuItemDescription = "Crisp romaine lettuce with grilled chicken, crunchy croutons, shaved parmesan, and Caesar dressing.", MenuItemCalories = 420, CategoryId = "C102", MenuItemUnitPrice = 13.90M, MenuItemImageUrl = "/images/menu/GrilledChicken.jpg" },
                    new MenuItem { MenuItemId = "M010", MenuItemName = "Quinoa Veggie Bowl", MenuItemDescription = "Nutrient-packed bowl of quinoa, roasted vegetables, fresh kale, and creamy tahini dressing.", MenuItemCalories = 400, CategoryId = "C102", MenuItemUnitPrice = 14.90M, MenuItemImageUrl = "/images/menu/QuinoaVeggie.jpg" },
                    new MenuItem { MenuItemId = "M011", MenuItemName = "Grilled Salmon Quinoa Bowl", MenuItemDescription = "Grilled salmon fillet on quinoa with avocado, edamame, and miso vinaigrette for a balanced meal.", MenuItemCalories = 540, CategoryId = "C102", MenuItemUnitPrice = 19.90M, MenuItemImageUrl = "/images/menu/GrilledSalmon.jpg" },
                    new MenuItem { MenuItemId = "M012", MenuItemName = "Yogurt + Granola Fruit Bowl", MenuItemDescription = "Smooth yogurt topped with crunchy granola and a colorful mix of seasonal fruits.", MenuItemCalories = 360, CategoryId = "C102", MenuItemUnitPrice = 11.90M, MenuItemImageUrl = "/images/menu/YogurtGranola.jpg" },
                    new MenuItem { MenuItemId = "M013", MenuItemName = "Lava Cake with Ice Cream", MenuItemDescription = "Warm, molten chocolate cake served with a scoop of vanilla ice cream for a perfect melt-in-mouth combo.", MenuItemCalories = 510, CategoryId = "C103", MenuItemUnitPrice = 12.90M, MenuItemImageUrl = "/images/menu/LavaCake.jpg" },
                    new MenuItem { MenuItemId = "M014", MenuItemName = "Matcha Cheesecake Tart", MenuItemDescription = "A rich and earthy matcha cheesecake nestled in a buttery tart crust perfect for green tea lovers.", MenuItemCalories = 430, CategoryId = "C103", MenuItemUnitPrice = 10.90M, MenuItemImageUrl = "/images/menu/MatchaCheesecake.jpg" },
                    new MenuItem { MenuItemId = "M015", MenuItemName = "Tiramisu Cup", MenuItemDescription = "Classic Italian delight layered with coffee-soaked sponge and whipped mascarpone cream.", MenuItemCalories = 460, CategoryId = "C103", MenuItemUnitPrice = 11.50M, MenuItemImageUrl = "/images/menu/TiramisuCup.jpg" },
                    new MenuItem { MenuItemId = "M016", MenuItemName = "Churro Bites with Dip", MenuItemDescription = "Crispy mini churros dusted with cinnamon sugar, served with rich chocolate dipping sauce.", MenuItemCalories = 390, CategoryId = "C103", MenuItemUnitPrice = 9.90M, MenuItemImageUrl = "/images/menu/ChurroBites.jpg" },
                    new MenuItem { MenuItemId = "M017", MenuItemName = "Americano (hot)", MenuItemDescription = "A bold, rich espresso diluted with hot water for a smooth, classic black coffee experience.", MenuItemCalories = 10, CategoryId = "C104", MenuItemUnitPrice = 7.00M, MenuItemImageUrl = "/images/menu/AmericanoHot.jpg" },
                    new MenuItem { MenuItemId = "M018", MenuItemName = "Americano (cold)", MenuItemDescription = "A chilled version of our Americano with a clean, robust flavor and refreshing finish.", MenuItemCalories = 10, CategoryId = "C104", MenuItemUnitPrice = 9.00M, MenuItemImageUrl = "/images/menu/AmericanoCold.jpg" },
                    new MenuItem { MenuItemId = "M019", MenuItemName = "Latte (hot)", MenuItemDescription = "Creamy steamed milk poured over a rich espresso shot, perfect for a cozy coffee break.", MenuItemCalories = 120, CategoryId = "C104", MenuItemUnitPrice = 8.50M, MenuItemImageUrl = "/images/menu/LatteHot.jpg" },
                    new MenuItem { MenuItemId = "M020", MenuItemName = "Latte (cold)", MenuItemDescription = "Iced espresso topped with cold milk for a cool and smooth latte experience.", MenuItemCalories = 120, CategoryId = "C104", MenuItemUnitPrice = 10.50M, MenuItemImageUrl = "/images/menu/LatteCold.jpg" },
                    new MenuItem { MenuItemId = "M021", MenuItemName = "Mocha (hot)", MenuItemDescription = "A luscious blend of espresso, chocolate, and steamed milk, topped with a light foam.", MenuItemCalories = 190, CategoryId = "C104", MenuItemUnitPrice = 9.00M, MenuItemImageUrl = "/images/menu/MochaHot.jpg" },
                    new MenuItem { MenuItemId = "M022", MenuItemName = "Mocha (cold)", MenuItemDescription = "Cool down with this decadent mix of espresso, chocolate syrup, and chilled milk.", MenuItemCalories = 190, CategoryId = "C104", MenuItemUnitPrice = 11.00M, MenuItemImageUrl = "/images/menu/MochaCold.jpg" },
                    new MenuItem { MenuItemId = "M023", MenuItemName = "Matcha Latte (hot)", MenuItemDescription = "Earthy matcha whisked into warm milk, delivering a creamy and energizing green tea delight.", MenuItemCalories = 160, CategoryId = "C104", MenuItemUnitPrice = 9.50M, MenuItemImageUrl = "/images/menu/MatchaHot.jpg" },
                    new MenuItem { MenuItemId = "M024", MenuItemName = "Matcha Latte (cold)", MenuItemDescription = "Iced matcha latte that balances the natural bitterness of green tea with velvety milk.", MenuItemCalories = 160, CategoryId = "C104", MenuItemUnitPrice = 11.50M, MenuItemImageUrl = "/images/menu/MatchaCold.jpg" },
                    new MenuItem { MenuItemId = "M025", MenuItemName = "Milk Tea (hot)", MenuItemDescription = "Smooth black tea infused with creamy milk, offering comfort in every warm sip.", MenuItemCalories = 140, CategoryId = "C104", MenuItemUnitPrice = 6.90M, MenuItemImageUrl = "/images/menu/MilkteaHot.jpg" },
                    new MenuItem { MenuItemId = "M026", MenuItemName = "Milk Tea (cold)", MenuItemDescription = "Chilled classic milk tea with a delicate balance of bold tea and sweet creaminess.", MenuItemCalories = 140, CategoryId = "C104", MenuItemUnitPrice = 8.90M, MenuItemImageUrl = "/images/menu/MilkteaCold.jpg" },
                    new MenuItem { MenuItemId = "M027", MenuItemName = "Mango Smoothies", MenuItemDescription = "Tropical mango blended into a thick, icy smoothie—sweet, tangy, and refreshing.", MenuItemCalories = 160, CategoryId = "C105", MenuItemUnitPrice = 12.90M, MenuItemImageUrl = "/images/menu/MangoSmoothie.jpg" },
                    new MenuItem { MenuItemId = "M028", MenuItemName = "Strawberry Smoothies", MenuItemDescription = "A fruity burst of real strawberries blended into a creamy smoothie base.", MenuItemCalories = 150, CategoryId = "C105", MenuItemUnitPrice = 12.90M, MenuItemImageUrl = "/images/menu/StrawberrySmoothie.jpg" },
                    new MenuItem { MenuItemId = "M029", MenuItemName = "Blueberry Smoothies", MenuItemDescription = "Packed with antioxidants, this smoothie is made with fresh blueberries and natural yogurt.", MenuItemCalories = 155, CategoryId = "C105", MenuItemUnitPrice = 12.90M, MenuItemImageUrl = "/images/menu/BlueberrySmoothie.jpg" },
                    new MenuItem { MenuItemId = "M030", MenuItemName = "Kiwi Smoothies", MenuItemDescription = "A zesty blend of kiwi and ice for a tart and revitalizing smoothie treat.", MenuItemCalories = 140, CategoryId = "C105", MenuItemUnitPrice = 12.90M, MenuItemImageUrl = "/images/menu/KiwiSmoothie.jpg" },
                    new MenuItem { MenuItemId = "M031", MenuItemName = "Oreo Milkshake", MenuItemDescription = "Classic cookies blended with creamy vanilla ice cream into a thick, indulgent shake.", MenuItemCalories = 310, CategoryId = "C105", MenuItemUnitPrice = 11.90M, MenuItemImageUrl = "/images/menu/OreoMilkshake.jpg" },
                    new MenuItem { MenuItemId = "M032", MenuItemName = "Strawberry Milkshake", MenuItemDescription = "Rich and creamy shake made with fresh strawberries and premium ice cream.", MenuItemCalories = 270, CategoryId = "C105", MenuItemUnitPrice = 11.90M, MenuItemImageUrl = "/images/menu/StrawberryMilkshake.jpg" },
                    new MenuItem { MenuItemId = "M033", MenuItemName = "Banana Milkshake", MenuItemDescription = "A smooth and satisfying blend of ripe bananas and cold milk for a wholesome treat.", MenuItemCalories = 290, CategoryId = "C105", MenuItemUnitPrice = 11.90M, MenuItemImageUrl = "/images/menu/BananaMilkshake.jpg" },
                    new MenuItem { MenuItemId = "M034", MenuItemName = "Lemon Mint Soda Mocktail", MenuItemDescription = "Zesty lemon and cooling mint combined with soda for a bubbly, invigorating refresher.", MenuItemCalories = 110, CategoryId = "C105", MenuItemUnitPrice = 14.90M, MenuItemImageUrl = "/images/menu/SodaLemon.jpg" },
                    new MenuItem { MenuItemId = "M035", MenuItemName = "Blueberry Fizz Soda Mocktail", MenuItemDescription = "Sweet blueberries and sparkling soda come together for a fun and fizzy drink.", MenuItemCalories = 115, CategoryId = "C105", MenuItemUnitPrice = 14.90M, MenuItemImageUrl = "/images/menu/SodaBlueberry.jpg" },
                    new MenuItem { MenuItemId = "M036", MenuItemName = "Butter Croissants", MenuItemDescription = "Flaky, golden-brown croissants with a buttery aroma and soft, layered interior.", MenuItemCalories = 290, CategoryId = "C106", MenuItemUnitPrice = 7.50M, MenuItemImageUrl = "/images/menu/ButterCroissant.jpg" },
                    new MenuItem { MenuItemId = "M037", MenuItemName = "Chocolate and Almond Croissants", MenuItemDescription = "Decadent croissants filled with rich chocolate and topped with toasted almonds.", MenuItemCalories = 340, CategoryId = "C106", MenuItemUnitPrice = 9.50M, MenuItemImageUrl = "/images/menu/ChocolateAlmond.jpg" },
                    new MenuItem { MenuItemId = "M038", MenuItemName = "Pistachio Croissants", MenuItemDescription = "A gourmet twist with nutty pistachio cream filling inside a buttery croissant shell.", MenuItemCalories = 350, CategoryId = "C106", MenuItemUnitPrice = 9.50M, MenuItemImageUrl = "/images/menu/PistachioCroissant.jpg" },
                    new MenuItem { MenuItemId = "M039", MenuItemName = "Banana Muffins", MenuItemDescription = "Moist and fluffy muffins made with real bananas for a naturally sweet flavor.", MenuItemCalories = 260, CategoryId = "C106", MenuItemUnitPrice = 8.50M, MenuItemImageUrl = "/images/menu/BananaMuffins.jpg" },
                    new MenuItem { MenuItemId = "M040", MenuItemName = "Blueberry Crumble Muffins", MenuItemDescription = "Juicy blueberries baked into soft muffins with a sweet crumble topping.", MenuItemCalories = 280, CategoryId = "C106", MenuItemUnitPrice = 8.50M, MenuItemImageUrl = "/images/menu/BlueberryCrumble.jpg" },
                    new MenuItem { MenuItemId = "M041", MenuItemName = "Chocolate Chip Muffins", MenuItemDescription = "Classic muffins loaded with gooey chocolate chips in every bite.", MenuItemCalories = 300, CategoryId = "C106", MenuItemUnitPrice = 7.50M, MenuItemImageUrl = "/images/menu/ChocolateChip.jpg" },
                    new MenuItem { MenuItemId = "M042", MenuItemName = "Cinnamon Rolls", MenuItemDescription = "Soft and sticky cinnamon rolls swirled with warm spices and drizzled with icing.", MenuItemCalories = 310, CategoryId = "C106", MenuItemUnitPrice = 6.90M, MenuItemImageUrl = "/images/menu/CinamonRolls.jpg" },
                    new MenuItem { MenuItemId = "M043", MenuItemName = "Scones with Jam and Cream", MenuItemDescription = "Traditional English scones served with fruity jam and a dollop of rich cream.", MenuItemCalories = 330, CategoryId = "C106", MenuItemUnitPrice = 9.50M, MenuItemImageUrl = "/images/menu/SconesJam.jpg" }
                };

                context.MenuItems.AddRange(menuItems);
                context.SaveChanges();
            }

            // Seed Order Customization Settings AFTER CategoryMenuItems exist
            if (!context.OrderCustomizationSettings.Any())
            {
                var customizations = new List<OrderCustomizationSettings>
    {
         // 🍳 All-Day Breakfast (C100)
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1001_M001", CustomizationName = "Extra Egg", CustomizationDescription = "Add an extra egg to your breakfast.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C100", MenuItemId = "M001" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1001_M002", CustomizationName = "Extra Egg", CustomizationDescription = "Add an extra egg to your breakfast.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C100", MenuItemId = "M002" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1001_M003", CustomizationName = "Extra Egg", CustomizationDescription = "Add an extra egg to your breakfast.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C100", MenuItemId = "M003" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1001_M005", CustomizationName = "Extra Egg", CustomizationDescription = "Add an extra egg to your breakfast.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C100", MenuItemId = "M005" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1002_M001", CustomizationName = "Add Chicken Sausage", CustomizationDescription = "Add a chicken sausage to your meal.", CustomizationAdditionalPrice = 3.00M, EligibleCategoryId = "C100", MenuItemId = "M001" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1002_M002", CustomizationName = "Add Chicken Sausage", CustomizationDescription = "Add a chicken sausage to your meal.", CustomizationAdditionalPrice = 3.00M, EligibleCategoryId = "C100", MenuItemId = "M002" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1002_M003", CustomizationName = "Add Chicken Sausage", CustomizationDescription = "Add a chicken sausage to your meal.", CustomizationAdditionalPrice = 3.00M, EligibleCategoryId = "C100", MenuItemId = "M003" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1002_M005", CustomizationName = "Add Chicken Sausage", CustomizationDescription = "Add a chicken sausage to your meal.", CustomizationAdditionalPrice = 3.00M, EligibleCategoryId = "C100", MenuItemId = "M005" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1003_M001", CustomizationName = "Gluten-Free Toast", CustomizationDescription = "Substitute regular toast with gluten-free.", CustomizationAdditionalPrice = 1.50M, EligibleCategoryId = "C100", MenuItemId = "M001" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1003_M002", CustomizationName = "Gluten-Free Toast", CustomizationDescription = "Substitute regular toast with gluten-free.", CustomizationAdditionalPrice = 1.50M, EligibleCategoryId = "C100", MenuItemId = "M002" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1003_M003", CustomizationName = "Gluten-Free Toast", CustomizationDescription = "Substitute regular toast with gluten-free.", CustomizationAdditionalPrice = 1.50M, EligibleCategoryId = "C100", MenuItemId = "M003" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1003_M005", CustomizationName = "Gluten-Free Toast", CustomizationDescription = "Substitute regular toast with gluten-free.", CustomizationAdditionalPrice = 1.50M, EligibleCategoryId = "C100", MenuItemId = "M005" },

    // 🥪 Sandwiches & Toasties (C101)
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1011_M006", CustomizationName = "Add Cheese", CustomizationDescription = "Add a slice of cheese.", CustomizationAdditionalPrice = 1.50M, EligibleCategoryId = "C101", MenuItemId = "M006" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1011_M007", CustomizationName = "Add Cheese", CustomizationDescription = "Add a slice of cheese.", CustomizationAdditionalPrice = 1.50M, EligibleCategoryId = "C101", MenuItemId = "M007" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1011_M008", CustomizationName = "Add Cheese", CustomizationDescription = "Add a slice of cheese.", CustomizationAdditionalPrice = 1.50M, EligibleCategoryId = "C101", MenuItemId = "M008" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1012_M006", CustomizationName = "No Mayonnaise", CustomizationDescription = "Remove mayonnaise from your sandwich.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C101", MenuItemId = "M006" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1012_M007", CustomizationName = "No Mayonnaise", CustomizationDescription = "Remove mayonnaise from your sandwich.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C101", MenuItemId = "M007" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1012_M008", CustomizationName = "No Mayonnaise", CustomizationDescription = "Remove mayonnaise from your sandwich.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C101", MenuItemId = "M008" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1013_M006", CustomizationName = "Whole Wheat Bread", CustomizationDescription = "Substitute with whole wheat bread.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C101", MenuItemId = "M006" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1013_M007", CustomizationName = "Whole Wheat Bread", CustomizationDescription = "Substitute with whole wheat bread.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C101", MenuItemId = "M007" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1013_M008", CustomizationName = "Whole Wheat Bread", CustomizationDescription = "Substitute with whole wheat bread.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C101", MenuItemId = "M008" },

    // 🥗 Salads & Healthy Bowls (C102)
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1021_M009", CustomizationName = "Add Avocado", CustomizationDescription = "Fresh avocado slices.", CustomizationAdditionalPrice = 2.50M, EligibleCategoryId = "C102", MenuItemId = "M009" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1021_M010", CustomizationName = "Add Avocado", CustomizationDescription = "Fresh avocado slices.", CustomizationAdditionalPrice = 2.50M, EligibleCategoryId = "C102", MenuItemId = "M010" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1021_M011", CustomizationName = "Add Avocado", CustomizationDescription = "Fresh avocado slices.", CustomizationAdditionalPrice = 2.50M, EligibleCategoryId = "C102", MenuItemId = "M011" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1021_M012", CustomizationName = "Add Avocado", CustomizationDescription = "Fresh avocado slices.", CustomizationAdditionalPrice = 2.50M, EligibleCategoryId = "C102", MenuItemId = "M012" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1022_M009", CustomizationName = "Double Protein", CustomizationDescription = "Double the protein portion.", CustomizationAdditionalPrice = 3.00M, EligibleCategoryId = "C102", MenuItemId = "M009" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1022_M010", CustomizationName = "Double Protein", CustomizationDescription = "Double the protein portion.", CustomizationAdditionalPrice = 3.00M, EligibleCategoryId = "C102", MenuItemId = "M010" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1022_M011", CustomizationName = "Double Protein", CustomizationDescription = "Double the protein portion.", CustomizationAdditionalPrice = 3.00M, EligibleCategoryId = "C102", MenuItemId = "M011" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1022_M012", CustomizationName = "Double Protein", CustomizationDescription = "Double the protein portion.", CustomizationAdditionalPrice = 3.00M, EligibleCategoryId = "C102", MenuItemId = "M012" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1023_M009", CustomizationName = "Dressing on Side", CustomizationDescription = "Dressing served on the side.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C102", MenuItemId = "M009" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1023_M010", CustomizationName = "Dressing on Side", CustomizationDescription = "Dressing served on the side.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C102", MenuItemId = "M010" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1023_M011", CustomizationName = "Dressing on Side", CustomizationDescription = "Dressing served on the side.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C102", MenuItemId = "M011" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1023_M012", CustomizationName = "Dressing on Side", CustomizationDescription = "Dressing served on the side.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C102", MenuItemId = "M012" },

    // 🍰 Signature Desserts (C103)
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1031_M013", CustomizationName = "Add Ice Cream", CustomizationDescription = "Top with vanilla ice cream.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C103", MenuItemId = "M013" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1031_M014", CustomizationName = "Add Ice Cream", CustomizationDescription = "Top with vanilla ice cream.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C103", MenuItemId = "M014" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1031_M015", CustomizationName = "Add Ice Cream", CustomizationDescription = "Top with vanilla ice cream.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C103", MenuItemId = "M015" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1031_M016", CustomizationName = "Add Ice Cream", CustomizationDescription = "Top with vanilla ice cream.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C103", MenuItemId = "M016" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1032_M013", CustomizationName = "Extra Chocolate Sauce", CustomizationDescription = "Add more chocolate sauce.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C103", MenuItemId = "M013" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1032_M014", CustomizationName = "Extra Chocolate Sauce", CustomizationDescription = "Add more chocolate sauce.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C103", MenuItemId = "M014" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1032_M015", CustomizationName = "Extra Chocolate Sauce", CustomizationDescription = "Add more chocolate sauce.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C103", MenuItemId = "M015" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1032_M016", CustomizationName = "Extra Chocolate Sauce", CustomizationDescription = "Add more chocolate sauce.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C103", MenuItemId = "M016" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1033_M013", CustomizationName = "Warm Up Dessert", CustomizationDescription = "Serve dessert warm.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C103", MenuItemId = "M013" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1033_M014", CustomizationName = "Warm Up Dessert", CustomizationDescription = "Serve dessert warm.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C103", MenuItemId = "M014" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1033_M015", CustomizationName = "Warm Up Dessert", CustomizationDescription = "Serve dessert warm.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C103", MenuItemId = "M015" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1033_M016", CustomizationName = "Warm Up Dessert", CustomizationDescription = "Serve dessert warm.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C103", MenuItemId = "M016" },

    // ☕ Coffee & Tea (C104)
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1041_M018", CustomizationName = "Less Ice", CustomizationDescription = "Less ice for iced drinks.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M018" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1041_M020", CustomizationName = "Less Ice", CustomizationDescription = "Less ice for iced drinks.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M020" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1041_M022", CustomizationName = "Less Ice", CustomizationDescription = "Less ice for iced drinks.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M022" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1041_M024", CustomizationName = "Less Ice", CustomizationDescription = "Less ice for iced drinks.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M024" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1041_M026", CustomizationName = "Less Ice", CustomizationDescription = "Less ice for iced drinks.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M026" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1042_M017", CustomizationName = "No Sugar", CustomizationDescription = "No added sugar.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M017" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1042_M018", CustomizationName = "No Sugar", CustomizationDescription = "No added sugar.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M018" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1042_M019", CustomizationName = "No Sugar", CustomizationDescription = "No added sugar.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M019" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1042_M020", CustomizationName = "No Sugar", CustomizationDescription = "No added sugar.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M020" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1042_M021", CustomizationName = "No Sugar", CustomizationDescription = "No added sugar.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M021" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1042_M022", CustomizationName = "No Sugar", CustomizationDescription = "No added sugar.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M022" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1042_M023", CustomizationName = "No Sugar", CustomizationDescription = "No added sugar.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M023" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1042_M024", CustomizationName = "No Sugar", CustomizationDescription = "No added sugar.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M024" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1042_M025", CustomizationName = "No Sugar", CustomizationDescription = "No added sugar.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M025" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1042_M026", CustomizationName = "No Sugar", CustomizationDescription = "No added sugar.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C104", MenuItemId = "M026" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1043_M017", CustomizationName = "Oat Milk", CustomizationDescription = "Substitute with oat milk.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M017" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1043_M018", CustomizationName = "Oat Milk", CustomizationDescription = "Substitute with oat milk.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M018" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1043_M019", CustomizationName = "Oat Milk", CustomizationDescription = "Substitute with oat milk.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M019" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1043_M020", CustomizationName = "Oat Milk", CustomizationDescription = "Substitute with oat milk.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M020" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1043_M021", CustomizationName = "Oat Milk", CustomizationDescription = "Substitute with oat milk.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M021" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1043_M022", CustomizationName = "Oat Milk", CustomizationDescription = "Substitute with oat milk.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M022" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1043_M023", CustomizationName = "Oat Milk", CustomizationDescription = "Substitute with oat milk.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M023" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1043_M024", CustomizationName = "Oat Milk", CustomizationDescription = "Substitute with oat milk.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M024" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1043_M025", CustomizationName = "Oat Milk", CustomizationDescription = "Substitute with oat milk.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M025" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1043_M026", CustomizationName = "Oat Milk", CustomizationDescription = "Substitute with oat milk.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M026" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1044_M017", CustomizationName = "Extra Shot", CustomizationDescription = "Add an extra espresso shot.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M017" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1044_M018", CustomizationName = "Extra Shot", CustomizationDescription = "Add an extra espresso shot.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M018" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1044_M019", CustomizationName = "Extra Shot", CustomizationDescription = "Add an extra espresso shot.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M019" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1044_M020", CustomizationName = "Extra Shot", CustomizationDescription = "Add an extra espresso shot.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M020" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1044_M021", CustomizationName = "Extra Shot", CustomizationDescription = "Add an extra espresso shot.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M021" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1044_M022", CustomizationName = "Extra Shot", CustomizationDescription = "Add an extra espresso shot.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M022" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1044_M023", CustomizationName = "Extra Shot", CustomizationDescription = "Add an extra espresso shot.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M023" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1044_M024", CustomizationName = "Extra Shot", CustomizationDescription = "Add an extra espresso shot.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M024" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1044_M025", CustomizationName = "Extra Shot", CustomizationDescription = "Add an extra espresso shot.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M025" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1044_M026", CustomizationName = "Extra Shot", CustomizationDescription = "Add an extra espresso shot.", CustomizationAdditionalPrice = 2.00M, EligibleCategoryId = "C104", MenuItemId = "M026" },

    // 🍹 Smoothies & Coolers (C105)
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1051_M027", CustomizationName = "Less Sweet", CustomizationDescription = "Less sweetener.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C105", MenuItemId = "M027" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1051_M028", CustomizationName = "Less Sweet", CustomizationDescription = "Less sweetener.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C105", MenuItemId = "M028" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1051_M029", CustomizationName = "Less Sweet", CustomizationDescription = "Less sweetener.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C105", MenuItemId = "M029" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1051_M030", CustomizationName = "Less Sweet", CustomizationDescription = "Less sweetener.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C105", MenuItemId = "M030" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1051_M031", CustomizationName = "Less Sweet", CustomizationDescription = "Less sweetener.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C105", MenuItemId = "M031" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1051_M032", CustomizationName = "Less Sweet", CustomizationDescription = "Less sweetener.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C105", MenuItemId = "M032" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1051_M033", CustomizationName = "Less Sweet", CustomizationDescription = "Less sweetener.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C105", MenuItemId = "M033" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1051_M034", CustomizationName = "Less Sweet", CustomizationDescription = "Less sweetener.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C105", MenuItemId = "M034" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1051_M035", CustomizationName = "Less Sweet", CustomizationDescription = "Less sweetener.", CustomizationAdditionalPrice = 0.00M, EligibleCategoryId = "C105", MenuItemId = "M035" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1052_M027", CustomizationName = "Add Chia Seeds", CustomizationDescription = "Add chia seeds for a health boost.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C105", MenuItemId = "M027" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1052_M028", CustomizationName = "Add Chia Seeds", CustomizationDescription = "Add chia seeds for a health boost.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C105", MenuItemId = "M028" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1052_M029", CustomizationName = "Add Chia Seeds", CustomizationDescription = "Add chia seeds for a health boost.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C105", MenuItemId = "M029" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1052_M030", CustomizationName = "Add Chia Seeds", CustomizationDescription = "Add chia seeds for a health boost.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C105", MenuItemId = "M030" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1052_M031", CustomizationName = "Add Chia Seeds", CustomizationDescription = "Add chia seeds for a health boost.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C105", MenuItemId = "M031" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1052_M032", CustomizationName = "Add Chia Seeds", CustomizationDescription = "Add chia seeds for a health boost.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C105", MenuItemId = "M032" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1052_M033", CustomizationName = "Add Chia Seeds", CustomizationDescription = "Add chia seeds for a health boost.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C105", MenuItemId = "M033" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1052_M034", CustomizationName = "Add Chia Seeds", CustomizationDescription = "Add chia seeds for a health boost.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C105", MenuItemId = "M034" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1052_M035", CustomizationName = "Add Chia Seeds", CustomizationDescription = "Add chia seeds for a health boost.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C105", MenuItemId = "M035" },

    // 🥐 Baked Goods & Pastries (C106)
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1061_M036", CustomizationName = "Warm Up", CustomizationDescription = "Serve pastry warm.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C106", MenuItemId = "M036" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1061_M037", CustomizationName = "Warm Up", CustomizationDescription = "Serve pastry warm.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C106", MenuItemId = "M037" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1061_M038", CustomizationName = "Warm Up", CustomizationDescription = "Serve pastry warm.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C106", MenuItemId = "M038" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1061_M039", CustomizationName = "Warm Up", CustomizationDescription = "Serve pastry warm.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C106", MenuItemId = "M039" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1061_M040", CustomizationName = "Warm Up", CustomizationDescription = "Serve pastry warm.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C106", MenuItemId = "M040" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1061_M041", CustomizationName = "Warm Up", CustomizationDescription = "Serve pastry warm.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C106", MenuItemId = "M041" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1061_M042", CustomizationName = "Warm Up", CustomizationDescription = "Serve pastry warm.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C106", MenuItemId = "M042" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1061_M043", CustomizationName = "Warm Up", CustomizationDescription = "Serve pastry warm.", CustomizationAdditionalPrice = 0.50M, EligibleCategoryId = "C106", MenuItemId = "M043" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1062_M036", CustomizationName = "Add Jam", CustomizationDescription = "Side of fruit jam.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C106", MenuItemId = "M036" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1062_M037", CustomizationName = "Add Jam", CustomizationDescription = "Side of fruit jam.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C106", MenuItemId = "M037" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1062_M038", CustomizationName = "Add Jam", CustomizationDescription = "Side of fruit jam.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C106", MenuItemId = "M038" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1062_M039", CustomizationName = "Add Jam", CustomizationDescription = "Side of fruit jam.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C106", MenuItemId = "M039" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1062_M040", CustomizationName = "Add Jam", CustomizationDescription = "Side of fruit jam.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C106", MenuItemId = "M040" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1062_M041", CustomizationName = "Add Jam", CustomizationDescription = "Side of fruit jam.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C106", MenuItemId = "M041" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1062_M042", CustomizationName = "Add Jam", CustomizationDescription = "Side of fruit jam.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C106", MenuItemId = "M042" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1062_M043", CustomizationName = "Add Jam", CustomizationDescription = "Side of fruit jam.", CustomizationAdditionalPrice = 1.00M, EligibleCategoryId = "C106", MenuItemId = "M043" },

    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1063_M036", CustomizationName = "Add Butter", CustomizationDescription = "Extra portion of butter.", CustomizationAdditionalPrice = 0.80M, EligibleCategoryId = "C106", MenuItemId = "M036" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1063_M037", CustomizationName = "Add Butter", CustomizationDescription = "Extra portion of butter.", CustomizationAdditionalPrice = 0.80M, EligibleCategoryId = "C106", MenuItemId = "M037" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1063_M038", CustomizationName = "Add Butter", CustomizationDescription = "Extra portion of butter.", CustomizationAdditionalPrice = 0.80M, EligibleCategoryId = "C106", MenuItemId = "M038" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1063_M039", CustomizationName = "Add Butter", CustomizationDescription = "Extra portion of butter.", CustomizationAdditionalPrice = 0.80M, EligibleCategoryId = "C106", MenuItemId = "M039" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1063_M040", CustomizationName = "Add Butter", CustomizationDescription = "Extra portion of butter.", CustomizationAdditionalPrice = 0.80M, EligibleCategoryId = "C106", MenuItemId = "M040" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1063_M041", CustomizationName = "Add Butter", CustomizationDescription = "Extra portion of butter.", CustomizationAdditionalPrice = 0.80M, EligibleCategoryId = "C106", MenuItemId = "M041" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1063_M042", CustomizationName = "Add Butter", CustomizationDescription = "Extra portion of butter.", CustomizationAdditionalPrice = 0.80M, EligibleCategoryId = "C106", MenuItemId = "M042" },
    new OrderCustomizationSettings { MenuItemCustomizationId = "OC1063_M043", CustomizationName = "Add Butter", CustomizationDescription = "Extra portion of butter.", CustomizationAdditionalPrice = 0.80M, EligibleCategoryId = "C106", MenuItemId = "M043" },
                };


                context.OrderCustomizationSettings.AddRange(customizations);
                context.SaveChanges();
                Console.WriteLine("Order Customization Settings seeded successfully.");
            }


            // --- Add only Sammy if not exists ---
            if (!context.Users.Any(u => u.UserId == "U101" || u.LoginEmailAddress == "tancaiyee2912@gmail.com"))
            {
                var sammy = new User
                {
                    UserId = "U101",
                    UserFullName = "Sammy Tan",
                    LoginEmailAddress = "tancaiyee2912@gmail.com",
                    HashedPassword = PasswordHasher.HashPassword("Tammy@123"), // ✅ hashed password
                    UserRoleId = "UR100",
                    UserProfileImageUrl = "wwwroot/images/sammyadmin.jpg"
                };

                context.Users.Add(sammy);
                context.SaveChanges();
            }


            // You can optionally seed more data here like Customers, CategoryMenuItems, etc.
        }
    }
}
