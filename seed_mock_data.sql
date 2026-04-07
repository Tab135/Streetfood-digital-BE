-- ============================================================
-- StreetFood Digital - Vietnamese Mock Data Seed Script
-- Password for ALL accounts: Password@123
-- Login via phone number (OTP flow) or password field
--
-- Requires: pgcrypto extension (for BCrypt password hashing)
-- Usage: psql -U <user> -d <dbname> -f seed_mock_data.sql
-- ============================================================

BEGIN;

CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ============================================================
-- ACCOUNTS SUMMARY
-- ============================================================
-- Admin:    0901234567 / admin@streetfood.vn
-- Vendor 1: 0912345678 / vendor1@streetfood.vn  (Quán Bún Bò Dì Hương)
-- Vendor 2: 0923456789 / vendor2@streetfood.vn  (Cơm Tấm Sài Gòn Anh Phúc)
-- Vendor 3: 0934567890 / vendor3@streetfood.vn  (Bánh Mì Ba Lan Chị Lan)
-- Manager 1: 0945678901 / manager1@streetfood.vn
-- Manager 2: 0956789012 / manager2@streetfood.vn
-- User 1:   0967890123 / nguyenvana@gmail.com
-- User 2:   0978901234 / tranthib@gmail.com
-- User 3:   0989012345 / levanc@gmail.com
-- User 4:   0990123456 / phamthid@gmail.com
-- User 5:   0901234568 / hoangvane@gmail.com
-- ============================================================

-- ============================================================
-- 1. CATEGORIES
-- ============================================================
INSERT INTO "Categories" ("CategoryId", "Name", "Description") VALUES
(1,  'Bún - Phở',    'Các món bún và phở truyền thống Việt Nam'),
(2,  'Cơm tấm',      'Cơm tấm sườn bì chả đặc trưng Sài Gòn'),
(3,  'Bánh mì',      'Bánh mì kẹp các loại nhân đa dạng'),
(4,  'Hải sản',      'Các món hải sản tươi sống hấp dẫn'),
(5,  'Đồ uống',      'Nước giải khát, sinh tố, trà sữa'),
(6,  'Bánh tráng',   'Bánh tráng nướng, cuốn, trộn'),
(7,  'Chè - Kem',    'Các loại chè và kem mát lạnh'),
(8,  'Xôi',          'Xôi các vị truyền thống'),
(9,  'Gỏi - Cuốn',   'Gỏi cuốn, gỏi trộn tươi ngon'),
(10, 'Lẩu - Nướng',  'Lẩu và các món nướng đặc sắc');

-- ============================================================
-- 2. TASTES
-- ============================================================
INSERT INTO "Tastes" ("TasteId", "Name", "Description") VALUES
(1, 'Cay',   'Vị cay nồng đặc trưng'),
(2, 'Ngọt',  'Vị ngọt thanh dịu'),
(3, 'Mặn',   'Vị mặn đậm đà'),
(4, 'Chua',  'Vị chua nhẹ kích thích'),
(5, 'Umami', 'Vị ngọt thịt đậm đà');

-- ============================================================
-- 3. BADGES
-- ============================================================
INSERT INTO "Badges" ("BadgeId", "BadgeName", "PointToGet", "IconUrl", "Description") VALUES
(1, 'Thực khách mới',   0,    'https://via.placeholder.com/100/FFD700?text=NEW',      'Chào mừng thành viên mới tham gia'),
(2, 'Tín đồ ẩm thực',  100,  'https://via.placeholder.com/100/FF6B35?text=100',      'Đã tích lũy 100 điểm thưởng'),
(3, 'Nhà phê bình',     250,  'https://via.placeholder.com/100/E74C3C?text=250',      'Đã tích lũy 250 điểm thưởng'),
(4, 'Vua đường phố',    500,  'https://via.placeholder.com/100/8E44AD?text=500',      'Đã tích lũy 500 điểm thưởng'),
(5, 'Huyền thoại',      1000, 'https://via.placeholder.com/100/2C3E50?text=1000',     'Đã tích lũy 1000 điểm thưởng');

-- ============================================================
-- 4. FEEDBACK TAGS
-- ============================================================
INSERT INTO "FeedbackTags" ("TagId", "TagName", "Description") VALUES
(1, 'Ngon',          'Món ăn rất ngon, chất lượng cao'),
(2, 'Sạch sẽ',       'Quán sạch sẽ, gọn gàng'),
(3, 'Phục vụ tốt',   'Nhân viên phục vụ nhanh nhẹn, chu đáo'),
(4, 'Giá hợp lý',    'Giá cả phải chăng, xứng đáng'),
(5, 'Không gian đẹp','Không gian thoáng mát, dễ chịu');

-- ============================================================
-- 5. USERS
-- Roles: User=0, Admin=1, Moderator=2, Vendor=3, Manager=4
-- Password: Password@123 (BCrypt hashed via pgcrypto)
-- ============================================================
INSERT INTO "Users" (
    "Id", "UserName", "Email", "Password", "PhoneNumber",
    "FirstName", "LastName", "Status", "Role", "Point",
    "EmailVerified", "MoneyBalance", "UserInfoSetup", "DietarySetup",
    "AvatarUrl", "CreatedAt", "UpdatedAt"
) VALUES
-- Admin
(1,  'admin_streetfood',  'admin@streetfood.vn',     crypt('Password@123', gen_salt('bf', 11)),
     '0901234567', 'Minh', 'Nguyễn', 'Active', 1, 0,
     true, 0.00, true, false, NULL, NOW() - INTERVAL '90 days', NOW()),

-- Vendors (Role = 3)
(2,  'vendor_bun_bo',     'vendor1@streetfood.vn',   crypt('Password@123', gen_salt('bf', 11)),
     '0912345678', 'Hương', 'Trần Thị', 'Active', 3, 0,
     true, 1500000.00, true, false, NULL, NOW() - INTERVAL '80 days', NOW()),

(3,  'vendor_com_tam',    'vendor2@streetfood.vn',   crypt('Password@123', gen_salt('bf', 11)),
     '0923456789', 'Phúc', 'Lê Văn', 'Active', 3, 0,
     true, 800000.00, true, false, NULL, NOW() - INTERVAL '60 days', NOW()),

(4,  'vendor_banh_mi',    'vendor3@streetfood.vn',   crypt('Password@123', gen_salt('bf', 11)),
     '0934567890', 'Lan', 'Phạm Thị', 'Active', 3, 0,
     true, 450000.00, true, false, NULL, NOW() - INTERVAL '50 days', NOW()),

-- Managers (Role = 4)
(5,  'manager_quan1',     'manager1@streetfood.vn',  crypt('Password@123', gen_salt('bf', 11)),
     '0945678901', 'Tuấn', 'Võ Anh', 'Active', 4, 0,
     true, 0.00, true, false, NULL, NOW() - INTERVAL '70 days', NOW()),

(6,  'manager_quan3',     'manager2@streetfood.vn',  crypt('Password@123', gen_salt('bf', 11)),
     '0956789012', 'Mai', 'Đặng Thị', 'Active', 4, 0,
     true, 0.00, true, false, NULL, NOW() - INTERVAL '45 days', NOW()),

-- Regular users (Role = 0)
(7,  'nguyen_van_an',     'nguyenvana@gmail.com',    crypt('Password@123', gen_salt('bf', 11)),
     '0967890123', 'An', 'Nguyễn Văn', 'Active', 0, 150,
     true, 50000.00, true, true, NULL, NOW() - INTERVAL '30 days', NOW()),

(8,  'tran_thi_binh',     'tranthib@gmail.com',      crypt('Password@123', gen_salt('bf', 11)),
     '0978901234', 'Bình', 'Trần Thị', 'Active', 0, 280,
     true, 120000.00, true, true, NULL, NOW() - INTERVAL '25 days', NOW()),

(9,  'le_van_cuong',      'levanc@gmail.com',        crypt('Password@123', gen_salt('bf', 11)),
     '0989012345', 'Cường', 'Lê Văn', 'Active', 0, 90,
     true, 30000.00, true, false, NULL, NOW() - INTERVAL '20 days', NOW()),

(10, 'pham_thi_dung',     'phamthid@gmail.com',      crypt('Password@123', gen_salt('bf', 11)),
     '0990123456', 'Dung', 'Phạm Thị', 'Active', 0, 350,
     true, 250000.00, true, true, NULL, NOW() - INTERVAL '40 days', NOW()),

(11, 'hoang_van_em',      'hoangvane@gmail.com',     crypt('Password@123', gen_salt('bf', 11)),
     '0901234568', 'Em', 'Hoàng Văn', 'Active', 0, 40,
     false, 0.00, true, false, NULL, NOW() - INTERVAL '5 days', NOW());

-- ============================================================
-- 6. USER DIETARY PREFERENCES
-- NOTE: actual PK column = userDietaryPreferencesId (camelCase)
--       actual FK column = dietaryPreferenceId (camelCase)
-- (References seeded DietaryPreferences: 1=An chay, 2=Cay, 3=Ngot, 4=Man, 5=Hai san)
-- ============================================================
INSERT INTO "UserDietaryPreferences" ("userDietaryPreferencesId", "UserId", "dietaryPreferenceId") VALUES
(1, 7,  2),  -- An: Cay
(2, 7,  3),  -- An: Ngot
(3, 8,  4),  -- Binh: Man
(4, 8,  5),  -- Binh: Hai san
(5, 10, 1),  -- Dung: An chay
(6, 10, 3);  -- Dung: Ngot

-- ============================================================
-- 7. VENDORS
-- ============================================================
INSERT INTO "Vendors" ("VendorId", "UserId", "Name", "CreatedAt", "UpdatedAt", "IsActive", "MoneyBalance") VALUES
(1, 2, 'Quán Bún Bò Dì Hương',          NOW() - INTERVAL '80 days', NOW(), true, 1500000.00),
(2, 3, 'Cơm Tấm Sài Gòn - Anh Phúc',   NOW() - INTERVAL '60 days', NOW(), true,  800000.00),
(3, 4, 'Bánh Mì Ba Lan - Chị Lan',      NOW() - INTERVAL '50 days', NOW(), true,  450000.00);

-- ============================================================
-- 8. BRANCHES
-- TierId: 1=Warning, 2=Silver, 3=Gold, 4=Diamond
-- ============================================================
INSERT INTO "Branches" (
    "BranchId", "VendorId", "ManagerId", "CreatedById",
    "Name", "PhoneNumber", "Email", "AddressDetail", "Ward", "City",
    "Lat", "Long", "CreatedAt", "UpdatedAt",
    "IsVerified", "AvgRating", "TotalReviewCount", "TotalRatingSum",
    "IsActive", "IsSubscribed", "SubscriptionExpiresAt",
    "TierId", "BatchReviewCount", "BatchRatingSum"
) VALUES
(1, 1, 5, 2,
 'Bún Bò Dì Hương - Quận 1', '0912345678', 'bunboq1@gmail.com',
 '123 Lý Tự Trọng', 'Bến Nghé', 'Hồ Chí Minh',
 10.77690, 106.70090, NOW() - INTERVAL '75 days', NOW(),
 true, 4.5, 20, 90, true, true,  NOW() + INTERVAL '30 days', 3, 5, 23),

(2, 1, 6, 2,
 'Bún Bò Dì Hương - Quận 3', '0912345679', 'bunboq3@gmail.com',
 '45 Võ Văn Tần', 'Võ Thị Sáu', 'Hồ Chí Minh',
 10.78310, 106.69170, NOW() - INTERVAL '60 days', NOW(),
 true, 4.2, 15, 63, true, false, NULL,                         2, 3, 13),

(3, 2, 5, 3,
 'Cơm Tấm Sài Gòn - Bình Thạnh', '0923456789', 'comtambt@gmail.com',
 '88 Đinh Tiên Hoàng', 'Đa Kao', 'Hồ Chí Minh',
 10.78890, 106.69520, NOW() - INTERVAL '55 days', NOW(),
 true, 4.7, 35, 165, true, true,  NOW() + INTERVAL '60 days', 4, 7, 34),

(4, 3, 6, 4,
 'Bánh Mì Ba Lan - Quận 5', '0934567890', 'banhmibalq5@gmail.com',
 '200 Trần Hưng Đạo', 'Cầu Ông Lãnh', 'Hồ Chí Minh',
 10.75730, 106.68360, NOW() - INTERVAL '45 days', NOW(),
 true, 4.0, 12, 48, true, true,  NOW() + INTERVAL '15 days', 2, 2, 8),

(5, 3, NULL, 4,
 'Bánh Mì Ba Lan - Quận 7', '0934567891', 'banhmibalq7@gmail.com',
 '55 Nguyễn Thị Thập', 'Tân Phú', 'Hồ Chí Minh',
 10.72420, 106.71670, NOW() - INTERVAL '20 days', NOW(),
 false, 0.0, 0, 0, false, false, NULL,                         2, 0, 0);

-- ============================================================
-- 9. BRANCH IMAGES
-- ============================================================
INSERT INTO "BranchImages" ("BranchImageId", "BranchId", "ImageUrl") VALUES
(1, 1, 'http://159.223.47.89:5298/uploads/branches/bunbo_q1_1.jpg'),
(2, 1, 'http://159.223.47.89:5298/uploads/branches/bunbo_q1_2.jpg'),
(3, 2, 'http://159.223.47.89:5298/uploads/branches/bunbo_q3_1.jpg'),
(4, 3, 'http://159.223.47.89:5298/uploads/branches/comtam_bt_1.jpg'),
(5, 3, 'http://159.223.47.89:5298/uploads/branches/comtam_bt_2.jpg'),
(6, 4, 'http://159.223.47.89:5298/uploads/branches/banhmi_q5_1.jpg'),
(7, 4, 'http://159.223.47.89:5298/uploads/branches/banhmi_q5_2.jpg');

-- ============================================================
-- 10. WORK SCHEDULES
-- Weekday: 0=Sunday, 1=Monday ... 6=Saturday
-- ============================================================
INSERT INTO "WorkSchedules" ("WorkScheduleId", "BranchId", "Weekday", "OpenTime", "CloseTime") VALUES
-- Branch 1 (Bún Bò Q1): Mon–Sat 06:00–21:00
(1,  1, 1, '06:00:00', '21:00:00'),
(2,  1, 2, '06:00:00', '21:00:00'),
(3,  1, 3, '06:00:00', '21:00:00'),
(4,  1, 4, '06:00:00', '21:00:00'),
(5,  1, 5, '06:00:00', '21:00:00'),
(6,  1, 6, '06:00:00', '20:00:00'),
-- Branch 2 (Bún Bò Q3): Daily 07:00–22:00
(7,  2, 0, '07:00:00', '22:00:00'),
(8,  2, 1, '07:00:00', '22:00:00'),
(9,  2, 2, '07:00:00', '22:00:00'),
(10, 2, 3, '07:00:00', '22:00:00'),
(11, 2, 4, '07:00:00', '22:00:00'),
(12, 2, 5, '07:00:00', '22:00:00'),
(13, 2, 6, '07:00:00', '21:00:00'),
-- Branch 3 (Cơm Tấm BT): Daily 06:00–14:00
(14, 3, 0, '06:00:00', '14:00:00'),
(15, 3, 1, '06:00:00', '14:00:00'),
(16, 3, 2, '06:00:00', '14:00:00'),
(17, 3, 3, '06:00:00', '14:00:00'),
(18, 3, 4, '06:00:00', '14:00:00'),
(19, 3, 5, '06:00:00', '14:00:00'),
(20, 3, 6, '06:00:00', '13:00:00'),
-- Branch 4 (Bánh Mì Q5): Mon–Fri 06:30–19:00
(21, 4, 1, '06:30:00', '19:00:00'),
(22, 4, 2, '06:30:00', '19:00:00'),
(23, 4, 3, '06:30:00', '19:00:00'),
(24, 4, 4, '06:30:00', '19:00:00'),
(25, 4, 5, '06:30:00', '19:00:00');

-- ============================================================
-- 11. DISHES
-- ============================================================
INSERT INTO "Dishes" (
    "DishId", "VendorId", "CategoryId", "Name", "Price",
    "Description", "ImageUrl", "IsSoldOut", "IsActive", "CreatedAt", "UpdatedAt"
) VALUES
-- Vendor 1 – Bún Bò
(1,  1, 1, 'Bún Bò Huế đặc biệt',    55000,
     'Nước lèo đậm đà, giò heo, chả cua thơm ngon', NULL, false, true, NOW() - INTERVAL '70 days', NOW()),
(2,  1, 1, 'Bún Bò Huế thường',       40000,
     'Bún bò Huế truyền thống chuẩn vị miền Trung', NULL, false, true, NOW() - INTERVAL '70 days', NOW()),
(3,  1, 1, 'Bún Bò Huế chay',         35000,
     'Bún bò chay với nấm rơm và đậu hũ', NULL, false, true, NOW() - INTERVAL '70 days', NOW()),
(4,  1, 5, 'Cà phê sữa đá',           25000,
     'Cà phê đậm pha với sữa đặc ông Thọ', NULL, false, true, NOW() - INTERVAL '70 days', NOW()),
(5,  1, 5, 'Nước chanh muối',          15000,
     'Chanh muối thanh mát giải nhiệt', NULL, false, true, NOW() - INTERVAL '70 days', NOW()),

-- Vendor 2 – Cơm Tấm
(6,  2, 2, 'Cơm Tấm Sườn Bì Chả',    65000,
     'Sườn nướng thơm, bì sợi, chả trứng đặc trưng', NULL, false, true, NOW() - INTERVAL '55 days', NOW()),
(7,  2, 2, 'Cơm Tấm Sườn Đặc Biệt',  75000,
     'Combo sườn, bì, chả, trứng ốp la đầy đặn', NULL, false, true, NOW() - INTERVAL '55 days', NOW()),
(8,  2, 2, 'Cơm Tấm Sườn Đơn',       50000,
     'Sườn nướng mật ong thơm, cơm tấm dẻo', NULL, false, true, NOW() - INTERVAL '55 days', NOW()),
(9,  2, 2, 'Cơm Tấm Chả Trứng',      45000,
     'Chả trứng hấp mềm ăn kèm cơm tấm nóng hổi', NULL, false, true, NOW() - INTERVAL '55 days', NOW()),
(10, 2, 5, 'Trà đá',                  10000,
     'Trà đá giải khát kèm cơm tấm', NULL, false, true, NOW() - INTERVAL '55 days', NOW()),
(11, 2, 5, 'Nước ngọt lon',           15000,
     'Pepsi, 7Up, Sting, Mirinda tùy chọn', NULL, false, true, NOW() - INTERVAL '55 days', NOW()),

-- Vendor 3 – Bánh Mì
(12, 3, 3, 'Bánh Mì Đặc Biệt',        30000,
     'Thịt nguội, pate, chả lụa, rau thơm đầy đặn', NULL, false, true, NOW() - INTERVAL '45 days', NOW()),
(13, 3, 3, 'Bánh Mì Xíu Mại',         25000,
     'Xíu mại viên tròn sốt cà chua nóng hổi', NULL, false, true, NOW() - INTERVAL '45 days', NOW()),
(14, 3, 3, 'Bánh Mì Trứng',           20000,
     'Trứng chiên ốp la kèm rau xà lách tươi', NULL, false, true, NOW() - INTERVAL '45 days', NOW()),
(15, 3, 3, 'Bánh Mì Bơ Kẹp Sốt',     18000,
     'Bơ muối phết đều với sốt mayo thơm béo', NULL, false, true, NOW() - INTERVAL '45 days', NOW()),
(16, 3, 5, 'Sữa đậu nành',            15000,
     'Sữa đậu nành tươi nguyên chất, nóng hoặc lạnh', NULL, false, true, NOW() - INTERVAL '45 days', NOW()),
(17, 3, 3, 'Bánh Mì Heo Quay',        35000,
     'Heo quay giòn rụm, da vàng óng thơm ngon', NULL, false, true, NOW() - INTERVAL '45 days', NOW()),
(18, 3, 3, 'Bánh Mì Gà Xé',           28000,
     'Gà xé sợi trộn sốt mayonnaise, hành tây', NULL, false, true, NOW() - INTERVAL '45 days', NOW()),
(19, 3, 3, 'Bánh Mì Bò Xào Lá Lốt',  32000,
     'Bò xào lá lốt thơm nồng kẹp bánh mì nóng', NULL, false, true, NOW() - INTERVAL '45 days', NOW()),
(20, 3, 3, 'Bánh Mì Chả Cá Chiên',   27000,
     'Chả cá chiên vàng giòn, ăn kèm dưa leo', NULL, false, true, NOW() - INTERVAL '45 days', NOW());

-- ============================================================
-- 12. DISH TASTES
-- ============================================================
INSERT INTO "DishTastes" ("DishTasteId", "DishId", "TasteId") VALUES
(1,  1,  1),  -- Bún Bò đặc biệt: Cay
(2,  1,  5),  -- Bún Bò đặc biệt: Umami
(3,  2,  1),  -- Bún Bò thường: Cay
(4,  2,  3),  -- Bún Bò thường: Mặn
(5,  3,  5),  -- Bún Bò chay: Umami
(6,  6,  3),  -- Cơm Tấm SBC: Mặn
(7,  6,  2),  -- Cơm Tấm SBC: Ngọt
(8,  7,  3),  -- Cơm Tấm đặc biệt: Mặn
(9,  7,  2),  -- Cơm Tấm đặc biệt: Ngọt
(10, 12, 3),  -- Bánh Mì đặc biệt: Mặn
(11, 12, 5),  -- Bánh Mì đặc biệt: Umami
(12, 13, 1),  -- Bánh Mì Xíu Mại: Cay
(13, 17, 3),  -- Bánh Mì Heo Quay: Mặn
(14, 19, 1),  -- Bánh Mì Bò Xào: Cay
(15, 19, 3);  -- Bánh Mì Bò Xào: Mặn

-- ============================================================
-- 13. DISH DIETARY PREFERENCES
-- NOTE: DishDietaryPreferences table was dropped by migration
--       20260317122725_AddVendorDietaryPreference. It no longer exists.
-- ============================================================

-- ============================================================
-- 14. VENDOR DIETARY PREFERENCES
-- ============================================================
INSERT INTO "VendorDietaryPreferences" ("VendorDietaryPreferenceId", "VendorId", "DietaryPreferenceId") VALUES
(1, 1, 1),  -- Bún Bò: An chay (có món chay)
(2, 1, 2),  -- Bún Bò: Cay
(3, 2, 3),  -- Cơm Tấm: Ngot
(4, 2, 4),  -- Cơm Tấm: Man
(5, 3, 3),  -- Bánh Mì: Ngot
(6, 3, 4);  -- Bánh Mì: Man

-- ============================================================
-- 15. BRANCH DISHES
-- ============================================================
INSERT INTO "BranchDishes" ("BranchId", "DishId", "IsSoldOut", "UpdatedAt") VALUES
-- Branch 1 (Bún Bò Q1) – all Vendor 1 dishes
(1, 1,  false, NOW()),
(1, 2,  false, NOW()),
(1, 3,  false, NOW()),
(1, 4,  false, NOW()),
(1, 5,  false, NOW()),
-- Branch 2 (Bún Bò Q3) – all Vendor 1 dishes, bún chay sold out
(2, 1,  false, NOW()),
(2, 2,  false, NOW()),
(2, 3,  true,  NOW()),
(2, 4,  false, NOW()),
(2, 5,  false, NOW()),
-- Branch 3 (Cơm Tấm BT) – all Vendor 2 dishes
(3, 6,  false, NOW()),
(3, 7,  false, NOW()),
(3, 8,  false, NOW()),
(3, 9,  false, NOW()),
(3, 10, false, NOW()),
(3, 11, false, NOW()),
-- Branch 4 (Bánh Mì Q5) – all Vendor 3 dishes
(4, 12, false, NOW()),
(4, 13, false, NOW()),
(4, 14, false, NOW()),
(4, 15, false, NOW()),
(4, 16, false, NOW()),
(4, 17, false, NOW()),
(4, 18, false, NOW()),
(4, 19, false, NOW()),
(4, 20, false, NOW());

-- ============================================================
-- 16. BRANCH REQUESTS (verified history)
-- Type 1 = registration; Status: 0=Pending, 1=Accept, 2=Reject
-- ============================================================
INSERT INTO "BranchRequests" (
    "BranchRequestId", "BranchId", "LicenseUrl", "Type", "Status",
    "RejectReason", "CreatedAt", "UpdatedAt"
) VALUES
(1, 1, '["http://159.223.47.89:5298/uploads/licenses/branch1_gpdkkd.jpg"]', 1, 1,
 NULL, NOW() - INTERVAL '75 days', NOW() - INTERVAL '73 days'),
(2, 2, '["http://159.223.47.89:5298/uploads/licenses/branch2_gpdkkd.jpg"]', 1, 1,
 NULL, NOW() - INTERVAL '60 days', NOW() - INTERVAL '58 days'),
(3, 3, '["http://159.223.47.89:5298/uploads/licenses/branch3_gpdkkd.jpg"]', 1, 1,
 NULL, NOW() - INTERVAL '55 days', NOW() - INTERVAL '53 days'),
(4, 4, '["http://159.223.47.89:5298/uploads/licenses/branch4_gpdkkd.jpg"]', 1, 1,
 NULL, NOW() - INTERVAL '45 days', NOW() - INTERVAL '43 days'),
(5, 5, '["http://159.223.47.89:5298/uploads/licenses/branch5_gpdkkd.jpg"]', 1, 0,
 NULL, NOW() - INTERVAL '20 days', NOW() - INTERVAL '20 days');

-- ============================================================
-- 17. VOUCHERS
-- Type: 'fixed' | 'percent'
-- ============================================================
INSERT INTO "Vouchers" (
    "VoucherId", "Name", "Description", "Type",
    "DiscountValue", "MinAmountRequired", "MaxDiscountValue",
    "StartDate", "EndDate", "ExpiredDate",
    "IsActive", "VoucherCode", "RedeemPoint", "Quantity", "UsedQuantity", "CampaignId"
) VALUES
(1, 'Giảm 10.000đ',
 'Giảm 10.000đ cho đơn hàng từ 50.000đ', 'fixed',
 10000, 50000, NULL,
 NOW() - INTERVAL '7 days', NOW() + INTERVAL '30 days', NOW() + INTERVAL '30 days',
 true, 'STREET10K', 0, 100, 15, NULL),

(2, 'Giảm 15% tối đa 30K',
 'Giảm 15% cho đơn từ 100.000đ, tối đa giảm 30.000đ', 'percent',
 15, 100000, 30000,
 NOW() - INTERVAL '3 days', NOW() + INTERVAL '60 days', NOW() + INTERVAL '60 days',
 true, 'STREET15P', 100, 50, 5, NULL),

(3, 'Chào mừng thành viên mới',
 'Ưu đãi 20.000đ cho lần đặt hàng đầu tiên', 'fixed',
 20000, 0, NULL,
 NOW() - INTERVAL '90 days', NOW() + INTERVAL '365 days', NOW() + INTERVAL '365 days',
 true, 'WELCOME20K', 0, 500, 8, NULL);

-- ============================================================
-- 18. USER VOUCHERS
-- ============================================================
INSERT INTO "UserVouchers" ("UserVoucherId", "UserId", "VoucherId", "Quantity", "IsAvailable") VALUES
(1, 7,  1, 1, true),   -- An: STREET10K
(2, 7,  3, 1, false),  -- An: WELCOME20K (đã dùng)
(3, 8,  1, 1, true),   -- Binh: STREET10K
(4, 8,  2, 1, true),   -- Binh: STREET15P
(5, 9,  3, 1, true),   -- Cuong: WELCOME20K
(6, 10, 2, 1, true);   -- Dung: STREET15P

-- ============================================================
-- 19. ORDERS
-- Status stored as string (HasConversion<string>):
-- Pending | AwaitingVendorConfirmation | Paid | Cancelled | Complete
-- ============================================================
INSERT INTO "Orders" (
    "OrderId", "UserId", "BranchId", "UserVoucherId", "Status",
    "Table", "PaymentMethod", "CompletionCode",
    "TotalAmount", "DiscountAmount", "FinalAmount",
    "IsTakeAway", "CreatedAt", "UpdatedAt"
) VALUES
-- Completed orders
(1, 7,  1, 2,    'Complete', NULL,     'cash',  'DONE2501', 95000,  20000, 75000,  true,  NOW() - INTERVAL '5 days',  NOW() - INTERVAL '5 days'),
(2, 8,  3, NULL, 'Complete', 'Bàn 3', 'cash',  'DONE2502', 140000, NULL,  140000, false, NOW() - INTERVAL '3 days',  NOW() - INTERVAL '3 days'),
(3, 9,  4, NULL, 'Complete', NULL,     'payos', 'DONE2503', 58000,  NULL,  58000,  true,  NOW() - INTERVAL '2 days',  NOW() - INTERVAL '2 days'),
-- Paid (awaiting pickup/serve)
(4, 10, 1, NULL, 'Paid',     'Bàn 1', 'cash',  NULL,       110000, NULL,  110000, false, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '30 minutes'),
-- Pending (just placed)
(5, 7,  3, NULL, 'Pending',  NULL,     'cash',  NULL,       65000,  NULL,  65000,  true,  NOW() - INTERVAL '10 minutes', NOW() - INTERVAL '10 minutes');

-- ============================================================
-- 20. ORDER DISHES
-- FK: (BranchId, DishId) → BranchDishes(BranchId, DishId)
-- PK: (OrderId, DishId)
-- ============================================================
INSERT INTO "OrderDishes" ("OrderId", "BranchId", "DishId", "Quantity", "CreatedAt", "UpdatedAt") VALUES
-- Order 1 (User An, Branch 1 – Bún Bò Q1)
(1, 1, 1, 2, NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),   -- 2× Bún Bò đặc biệt
(1, 1, 4, 1, NOW() - INTERVAL '5 days', NOW() - INTERVAL '5 days'),   -- 1× Cà phê sữa đá
-- Order 2 (User Binh, Branch 3 – Cơm Tấm BT)
(2, 3, 6, 1, NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),   -- 1× Cơm Tấm SBC
(2, 3, 7, 1, NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),   -- 1× Cơm Tấm đặc biệt
(2, 3, 10, 2, NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),  -- 2× Trà đá
-- Order 3 (User Cuong, Branch 4 – Bánh Mì Q5)
(3, 4, 12, 1, NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),  -- 1× Bánh Mì đặc biệt
(3, 4, 13, 1, NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),  -- 1× Bánh Mì Xíu Mại
(3, 4, 16, 1, NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),  -- 1× Sữa đậu nành
-- Order 4 (User Dung, Branch 1 – Bún Bò Q1)
(4, 1, 1, 1, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '1 hour'),   -- 1× Bún Bò đặc biệt
(4, 1, 2, 1, NOW() - INTERVAL '1 hour', NOW() - INTERVAL '1 hour'),   -- 1× Bún Bò thường
-- Order 5 (User An, Branch 3 – Cơm Tấm BT)
(5, 3, 8, 1, NOW() - INTERVAL '10 minutes', NOW() - INTERVAL '10 minutes');  -- 1× Cơm Tấm Sườn Đơn

-- ============================================================
-- 21. FEEDBACKS (only for completed orders)
-- ============================================================
INSERT INTO "Feedbacks" (
    "FeedbackId", "UserId", "BranchId", "DishId", "OrderId",
    "Rating", "Comment", "CreatedAt", "UpdatedAt"
) VALUES
(1, 7, 1, 1,    1, 5,
 'Bún bò Huế ngon cực kỳ! Nước lèo đậm đà, thịt mềm tan, giò heo béo bùi. Quán sạch sẽ thoáng mát, nhất định sẽ quay lại!',
 NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days'),

(2, 7, 1, NULL, 1, 4,
 'Phục vụ nhanh, thái độ thân thiện. Không gian quán rộng, giá cả hợp lý. Rất đáng để ghé thử.',
 NOW() - INTERVAL '4 days', NOW() - INTERVAL '4 days'),

(3, 8, 3, 6,    2, 5,
 'Cơm tấm sườn nướng thơm phức! Sườn mềm ngấm gia vị, nước mắm pha chua ngọt chuẩn vị Sài Gòn. Xuất sắc!',
 NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),

(4, 8, 3, NULL, 2, 4,
 'Quán đông khách nhưng phục vụ vẫn nhanh, không phải chờ lâu. Không gian rộng thoáng, sạch sẽ.',
 NOW() - INTERVAL '2 days', NOW() - INTERVAL '2 days'),

(5, 9, 4, 12,   3, 4,
 'Bánh mì đặc biệt nhiều nhân, vỏ bánh giòn tan. Nhân viên thân thiện nhiệt tình. Sẽ ghé lại thường xuyên.',
 NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day');

-- ============================================================
-- 22. FEEDBACK IMAGES
-- ============================================================
INSERT INTO "FeedbackImages" ("FeedbackImageId", "FeedbackId", "ImageUrl") VALUES
(1, 1, 'http://159.223.47.89:5298/uploads/feedback/fb1_bunbo.jpg'),
(2, 3, 'http://159.223.47.89:5298/uploads/feedback/fb3_comtam1.jpg'),
(3, 3, 'http://159.223.47.89:5298/uploads/feedback/fb3_comtam2.jpg'),
(4, 5, 'http://159.223.47.89:5298/uploads/feedback/fb5_banhmi.jpg');

-- ============================================================
-- 23. FEEDBACK TAG ASSOCIATIONS
-- ============================================================
INSERT INTO "FeedbackTagAssociations" ("FeedbackTagId", "FeedbackId", "TagId") VALUES
(1, 1, 1),  -- fb1: Ngon
(2, 1, 2),  -- fb1: Sạch sẽ
(3, 2, 3),  -- fb2: Phục vụ tốt
(4, 2, 4),  -- fb2: Giá hợp lý
(5, 3, 1),  -- fb3: Ngon
(6, 3, 5),  -- fb3: Không gian đẹp
(7, 4, 3),  -- fb4: Phục vụ tốt
(8, 5, 1),  -- fb5: Ngon
(9, 5, 3);  -- fb5: Phục vụ tốt

-- ============================================================
-- 24. FEEDBACK VOTES
-- VoteType: Up=1, Down=-1
-- ============================================================
INSERT INTO "FeedbackVotes" ("FeedbackVoteId", "FeedbackId", "UserId", "VoteType", "CreatedAt", "UpdatedAt") VALUES
(1, 1, 8,  1, NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),  -- Binh vote up fb1
(2, 1, 9,  1, NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),  -- Cuong vote up fb1
(3, 1, 10, 1, NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),  -- Dung vote up fb1
(4, 3, 7,  1, NOW() - INTERVAL '1 day',  NOW() - INTERVAL '1 day'),   -- An vote up fb3
(5, 3, 9,  1, NOW() - INTERVAL '1 day',  NOW() - INTERVAL '1 day'),   -- Cuong vote up fb3
(6, 5, 8,  1, NOW() - INTERVAL '12 hours', NOW() - INTERVAL '12 hours'); -- Binh vote up fb5

-- ============================================================
-- 25. VENDOR REPLIES
-- ============================================================
INSERT INTO "VendorReplies" ("VendorReplyId", "FeedbackId", "UserId", "Content", "CreatedAt", "UpdatedAt") VALUES
(1, 1, 2,
 'Cảm ơn bạn An đã ghé thăm quán và để lại đánh giá rất tốt! Chúng tôi rất vui khi bạn hài lòng với chất lượng bún bò. Hẹn gặp lại bạn lần sau nhé! 🍜',
 NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'),

(2, 3, 3,
 'Cảm ơn bạn Bình rất nhiều! Chúng tôi sẽ tiếp tục cố gắng duy trì chất lượng để mang đến những bữa cơm tấm ngon nhất cho khách hàng. Hẹn gặp lại! 🍚',
 NOW() - INTERVAL '1 day', NOW() - INTERVAL '1 day');

-- ============================================================
-- 26. USER BADGES
-- ============================================================
INSERT INTO "UserBadges" ("UserId", "BadgeId", "CreatedAt") VALUES
(7,  1, NOW() - INTERVAL '30 days'),   -- An: Thực khách mới
(7,  2, NOW() - INTERVAL '10 days'),   -- An: Tín đồ ẩm thực (150 pts)
(8,  1, NOW() - INTERVAL '25 days'),   -- Binh: Thực khách mới
(8,  2, NOW() - INTERVAL '15 days'),   -- Binh: Tín đồ ẩm thực
(8,  3, NOW() - INTERVAL '5 days'),    -- Binh: Nhà phê bình (280 pts)
(9,  1, NOW() - INTERVAL '20 days'),   -- Cuong: Thực khách mới
(10, 1, NOW() - INTERVAL '40 days'),   -- Dung: Thực khách mới
(10, 2, NOW() - INTERVAL '25 days'),   -- Dung: Tín đồ ẩm thực
(10, 3, NOW() - INTERVAL '10 days'),   -- Dung: Nhà phê bình (350 pts)
(11, 1, NOW() - INTERVAL '5 days');    -- Em: Thực khách mới

-- ============================================================
-- 27. NOTIFICATIONS
-- Type is INTEGER: 0=NewFeedback, 1=VendorReply, 2=OrderStatusUpdate
-- ============================================================
INSERT INTO "Notifications" (
    "NotificationId", "UserId", "Type", "Title", "Message",
    "ReferenceId", "IsRead", "CreatedAt"
) VALUES
(1, 7,  2, 'Đơn hàng hoàn thành',
 'Đơn hàng #1 của bạn đã hoàn thành. Cảm ơn bạn đã sử dụng dịch vụ!',
 1, true,  NOW() - INTERVAL '5 days'),

(2, 7,  1, 'Quán đã phản hồi đánh giá',
 'Quán Bún Bò Dì Hương đã phản hồi đánh giá của bạn. Nhấn để xem.',
 1, false, NOW() - INTERVAL '3 days'),

(3, 7,  2, 'Đơn hàng đang chờ xử lý',
 'Đơn hàng #5 của bạn đã được đặt. Vui lòng chờ quán xác nhận.',
 5, false, NOW() - INTERVAL '10 minutes'),

(4, 8,  2, 'Đơn hàng hoàn thành',
 'Đơn hàng #2 của bạn đã hoàn thành. Cảm ơn bạn!',
 2, true,  NOW() - INTERVAL '3 days'),

(5, 8,  1, 'Quán đã phản hồi đánh giá',
 'Cơm Tấm Sài Gòn - Anh Phúc đã phản hồi đánh giá của bạn.',
 3, false, NOW() - INTERVAL '1 day'),

(6, 9,  2, 'Đơn hàng hoàn thành',
 'Đơn hàng #3 của bạn đã hoàn thành. Cảm ơn bạn!',
 3, false, NOW() - INTERVAL '2 days'),

(7, 10, 2, 'Đơn hàng đang được phục vụ',
 'Đơn hàng #4 đã được xác nhận và đang được chuẩn bị.',
 4, false, NOW() - INTERVAL '30 minutes');

-- ============================================================
-- 28. CAMPAIGNS
-- CreatedByBranchId / CreatedByVendorId are nullable (system campaign = both NULL)
-- Status: 'Active' | 'Ended' | 'Upcoming' | 'Cancelled'
-- isJoined (returned by API): computed per-user/vendor from BranchCampaigns
-- ============================================================
INSERT INTO "Campaigns" (
    "CampaignId", "CreatedByBranchId", "CreatedByVendorId",
    "Name", "Description", "TargetSegment",
    "RegistrationStartDate", "RegistrationEndDate",
    "StartDate", "EndDate",
    "CreatedAt", "UpdatedAt"
) VALUES
-- Campaign 1: Active system campaign (isJoined: true for branches 1,3 via BranchCampaigns)
(1, NULL, NULL,
 'Lễ Hội Ẩm Thực Đường Phố Hè 2026',
 'Sự kiện quảng bá ẩm thực đường phố TP.HCM mùa hè 2026. Các gian hàng tham gia sẽ được hiển thị nổi bật và tặng voucher cho khách hàng.',
 'Tất cả',
 NOW() - INTERVAL '20 days', NOW() - INTERVAL '5 days',
 NOW() + INTERVAL '5 days',  NOW() + INTERVAL '35 days',
 NOW() - INTERVAL '25 days', NOW() - INTERVAL '5 days'),

-- Campaign 2: Active vendor campaign — Bún Bò (isJoined: true for branch 1)
(2, 1, 1,
 'Khuyến Mãi Sinh Nhật Quán Bún Bò Dì Hương',
 'Nhân dịp kỷ niệm 1 năm thành lập, Bún Bò Dì Hương giảm giá đặc biệt cho tất cả các món bún và tặng voucher cho khách hàng thân thiết.',
 'Khách hàng thường xuyên',
 NOW() - INTERVAL '10 days', NOW() - INTERVAL '3 days',
 NOW() + INTERVAL '2 days',  NOW() + INTERVAL '17 days',
 NOW() - INTERVAL '12 days', NOW() - INTERVAL '3 days'),

-- Campaign 3: Active vendor campaign — Cơm Tấm (isJoined: true for branch 3)
(3, 3, 2,
 'Combo Cơm Tấm Cuối Tuần Giá Rẻ',
 'Mỗi cuối tuần, thưởng thức combo cơm tấm đặc biệt với giá ưu đãi. Đặt hàng qua app nhận ngay voucher giảm giá cho lần tiếp theo.',
 'Khách hàng mới',
 NOW() - INTERVAL '5 days', NOW() + INTERVAL '2 days',
 NOW() + INTERVAL '7 days',  NOW() + INTERVAL '37 days',
 NOW() - INTERVAL '7 days',  NOW() - INTERVAL '2 days'),

-- Campaign 4: Second active system campaign — no branches joined (tests isJoined:false)
(4, NULL, NULL,
 'Tuần Lễ Khám Phá Ẩm Thực Mới',
 'Chương trình khuyến khích thực khách thử những món ăn chưa từng ăn. Ghé các quán mới đăng ký trên app và nhận điểm thưởng đặc biệt.',
 'Tất cả',
 NOW() - INTERVAL '3 days', NOW() + INTERVAL '4 days',
 NOW() + INTERVAL '7 days',  NOW() + INTERVAL '21 days',
 NOW() - INTERVAL '5 days', NOW()),

-- Campaign 5: Ended vendor campaign — Bánh Mì (tests expired/past campaign)
(5, 4, 3,
 'Khai Trương Chi Nhánh Bánh Mì Ba Lan Q5',
 'Ưu đãi khai trương! Giảm 30% toàn bộ menu tại chi nhánh mới Q5 trong suốt tuần đầu tiên.',
 'Tất cả',
 NOW() - INTERVAL '60 days', NOW() - INTERVAL '53 days',
 NOW() - INTERVAL '50 days', NOW() - INTERVAL '43 days',
 NOW() - INTERVAL '65 days', NOW() - INTERVAL '43 days');

-- ============================================================
-- 29. BRANCH CAMPAIGNS
-- Status: 'Pending' | 'paid' | 'active' | 'rejected'
-- ============================================================
INSERT INTO "BranchCampaigns" ("Id", "CampaignId", "BranchId", "IsActive", "JoinedAt") VALUES
-- Campaign 1 (Lễ Hội Hè): 3 branches joined
(1, 1, 1, true,  NOW() - INTERVAL '18 days'),  -- Bún Bò Q1: active
(2, 1, 3, true,  NOW() - INTERVAL '17 days'),  -- Cơm Tấm BT: active
(3, 1, 4, false, NOW() - INTERVAL '8 days'),   -- Bánh Mì Q5: chờ duyệt

-- Campaign 2 (Sinh Nhật Bún Bò): 2 branches của Vendor 1
(4, 2, 1, true,  NOW() - INTERVAL '9 days'),   -- Bún Bò Q1: active
(5, 2, 2, false, NOW() - INTERVAL '8 days'),   -- Bún Bò Q3: chưa active

-- Campaign 3 (Cơm Tấm Cuối Tuần): 1 branch
(6, 3, 3, true,  NOW() - INTERVAL '4 days'),   -- Cơm Tấm BT: active

-- Campaign 4 (Tuần Khám Phá): no branches — tests isJoined:false
-- (intentionally empty)

-- Campaign 5 (Khai Trương Bánh Mì — đã kết thúc)
(7, 5, 4, false, NOW() - INTERVAL '48 days');  -- Bánh Mì Q5: inactive

-- ============================================================
-- 30. CAMPAIGN-LINKED VOUCHERS (new, beyond the 3 existing ones)
-- ============================================================
INSERT INTO "Vouchers" (
    "VoucherId", "Name", "Description", "Type",
    "DiscountValue", "MinAmountRequired", "MaxDiscountValue",
    "StartDate", "EndDate", "ExpiredDate",
    "IsActive", "VoucherCode", "RedeemPoint", "Quantity", "UsedQuantity", "CampaignId"
) VALUES
(4, 'Voucher Lễ Hội Ẩm Thực 25K',
 'Giảm 25.000đ cho đơn từ 80.000đ, áp dụng tại các gian hàng tham gia Lễ Hội Hè 2026',
 'fixed', 25000, 80000, NULL,
 NOW() + INTERVAL '5 days', NOW() + INTERVAL '35 days', NOW() + INTERVAL '35 days',
 true, 'LEHOI25K', 0, 200, 0, 1),

(5, 'Voucher Sinh Nhật Bún Bò 20%',
 'Giảm 20% tối đa 40.000đ cho đơn từ 80.000đ, chỉ áp dụng tại Bún Bò Dì Hương',
 'percent', 20, 80000, 40000,
 NOW() + INTERVAL '2 days', NOW() + INTERVAL '17 days', NOW() + INTERVAL '17 days',
 true, 'BUNBO20P', 0, 100, 3, 2),

(6, 'Voucher Cơm Tấm Cuối Tuần 15K',
 'Giảm 15.000đ áp dụng thứ 7 và Chủ nhật tại Cơm Tấm Sài Gòn',
 'fixed', 15000, 60000, NULL,
 NOW() + INTERVAL '7 days', NOW() + INTERVAL '37 days', NOW() + INTERVAL '37 days',
 true, 'COMTAM15K', 0, 150, 0, 3);

-- ============================================================
-- 31. USER VOUCHERS (campaign vouchers for existing users)
-- ============================================================
INSERT INTO "UserVouchers" ("UserVoucherId", "UserId", "VoucherId", "Quantity", "IsAvailable") VALUES
(7,  7,  4, 1, true),   -- An: Voucher Lễ Hội 25K
(8,  8,  4, 1, true),   -- Binh: Voucher Lễ Hội 25K
(9,  8,  5, 1, true),   -- Binh: Voucher Sinh Nhật Bún Bò 20%
(10, 10, 4, 1, true),   -- Dung: Voucher Lễ Hội 25K
(11, 9,  6, 1, true);   -- Cuong: Voucher Cơm Tấm Cuối Tuần 15K

-- ============================================================
-- 32. QUESTS
-- Tests: public list pagination, with/without imageUrl, linked/unlinked campaign,
--        active vs inactive (expired), all task count variations
-- ============================================================
INSERT INTO "Quests" (
    "QuestId", "Title", "Description", "ImageUrl",
    "IsActive", "IsStandalone", "CampaignId",
    "CreatedAt", "UpdatedAt"
) VALUES
-- Quest 1: Linked to system Campaign 1 — campaign quest, active, 3 tasks, has imageUrl
--   Tests: campaignId != null, image header, IN_PROGRESS (User An) + COMPLETED (User Dung)
(1,
 'Khám Phá Ẩm Thực Đường Phố HCM',
 'Ghé thăm các quán ẩm thực đường phố, để lại đánh giá và chia sẻ trải nghiệm để nhận phần thưởng hấp dẫn từ Lễ Hội Ẩm Thực Hè 2026.',
 'https://lowca-s3-bucket.s3.ap-southeast-1.amazonaws.com/quests/2c036fc6-f917-4191-804a-e64f76c927ce_images.jpeg',
 true, false, 1,
 NOW() - INTERVAL '5 days',  NULL),

-- Quest 2: Standalone — active, 2 tasks, no imageUrl
--   Tests: campaignId == null, fallback header, COMPLETED (User Binh)
(2,
 'Tín Đồ Bánh Mì Sài Gòn',
 'Khám phá các tiệm bánh mì ngon nhất HCM. Đặt hàng và chia sẻ để chứng tỏ bạn là tín đồ bánh mì thực thụ!',
 NULL,
 true, true, NULL,
 NOW() - INTERVAL '10 days', NULL),

-- Quest 3: Standalone — active, 2 tasks, has imageUrl
--   Tests: REVIEW + SHARE task types, BADGE reward, IN_PROGRESS with 0 progress (User An)
(3,
 'Thám Tử Đường Phố',
 'Viết đánh giá chi tiết và chia sẻ các quán ăn yêu thích của bạn với bạn bè để nhận phần thưởng đặc biệt.',
 'https://lowca-s3-bucket.s3.ap-southeast-1.amazonaws.com/quests/2c036fc6-f917-4191-804a-e64f76c927ce_images.jpeg',
 true, true, NULL,
 NOW() - INTERVAL '15 days', NULL),

-- Quest 4: Standalone — active, 1 task, no imageUrl
--   Tests: single-task quest, VOUCHER reward, not yet enrolled by any user
--   (Previously linked to vendor Campaign 3 — fixed: vendor campaigns cannot have quests)
(4,
 'Vua Cơm Tấm Sài Gòn',
 'Chứng tỏ tình yêu với cơm tấm Sài Gòn! Đặt hàng đủ số lượng và nhận ngay voucher đặc biệt.',
 NULL,
 true, true, NULL,
 NOW() - INTERVAL '2 days',  NULL),

-- Quest 5: Standalone — EXPIRED (isActive:false)
--   Tests: expired quest rendering, EXPIRED user status (User Cường)
(5,
 'Tiệc Tất Niên Ẩm Thực 2025',
 'Sự kiện kỷ niệm cuối năm đã kết thúc. Cảm ơn tất cả thực khách đã tham gia!',
 NULL,
 false, true, NULL,
 NOW() - INTERVAL '65 days', NOW() - INTERVAL '30 days');

-- ============================================================
-- 33. QUEST TASKS
-- Covers all QuestTaskType values: REVIEW, ORDER_AMOUNT, SHARE, CREATE_GHOST_PIN
-- Covers all QuestRewardType values: BADGE, POINTS, VOUCHER
-- ============================================================
INSERT INTO "QuestTasks" (
    "QuestTaskId", "QuestId", "Type", "TargetValue", "Description", "RewardType", "RewardValue"
) VALUES
-- Quest 1 tasks (3 tasks — full coverage of all task & reward types)
(1,  1, 'CREATE_GHOST_PIN', 3,      'Gắn địa điểm 3 quán ẩm thực mới trên bản đồ',                 'POINTS',  50),
(2,  1, 'REVIEW',           2,      'Viết 2 đánh giá chi tiết về quán ăn', 'BADGE',   2),
(3,  1, 'ORDER_AMOUNT',     200000, 'Đặt hàng tổng cộng 200.000đ qua app trong thời gian sự kiện',  'VOUCHER', 1),

-- Quest 2 tasks (2 tasks — SHARE + ORDER_AMOUNT, POINTS reward)
(4,  2, 'SHARE',        3,      'Chia sẻ 3 tiệm bánh mì yêu thích với bạn bè qua app',           'POINTS',  30),
(5,  2, 'ORDER_AMOUNT', 100000, 'Đặt hàng tổng cộng 100.000đ tại tiệm bánh mì',                  'POINTS',  50),

-- Quest 3 tasks (2 tasks — REVIEW + SHARE, POINTS + BADGE reward)
(6,  3, 'REVIEW',       1,      'Viết 1 đánh giá chi tiết',                     'POINTS',  20),
(7,  3, 'SHARE',        2,      'Chia sẻ 2 quán ăn yêu thích với bạn bè qua app',                'BADGE',   1),

-- Quest 4 tasks (1 task — ORDER_AMOUNT, VOUCHER reward)
(8,  4, 'ORDER_AMOUNT', 300000, 'Đặt hàng tổng cộng 300.000đ tại các quán cơm tấm trong tháng', 'VOUCHER', 1),

-- Quest 5 tasks (2 tasks — expired quest, partial completion history)
(9,  5, 'CREATE_GHOST_PIN', 5,      'Gắn địa điểm 5 quán ẩm thực trong tuần cuối năm',            'POINTS',  100),
(10, 5, 'ORDER_AMOUNT',     500000, 'Đặt hàng tổng cộng 500.000đ trong tháng 12',                 'VOUCHER', 2);

-- ============================================================
-- 34. USER QUESTS
-- Covers all UserQuestStatus values: IN_PROGRESS, COMPLETED, EXPIRED, STOPPED
-- User 7  (An):    Quest 1 IN_PROGRESS (2/3 tasks partially done)
-- User 8  (Binh):  Quest 2 COMPLETED (both tasks done)
-- User 7  (An):    Quest 3 IN_PROGRESS (0/2 tasks started)
-- User 10 (Dung):  Quest 1 COMPLETED (all tasks done)
-- User 9  (Cường): Quest 5 EXPIRED (partial progress)
-- User 11 (Hoàng): Quest 4 STOPPED (gave up after partial progress)
-- ============================================================
INSERT INTO "UserQuests" (
    "UserQuestId", "UserId", "QuestId", "Status", "StartedAt", "CompletedAt"
) VALUES
(1, 7,  1, 'IN_PROGRESS', NOW() - INTERVAL '3 days',  NULL),                        -- An: Quest 1 đang thực hiện
(2, 8,  2, 'COMPLETED',   NOW() - INTERVAL '8 days',  NOW() - INTERVAL '2 days'),   -- Binh: Quest 2 hoàn thành
(3, 7,  3, 'IN_PROGRESS', NOW() - INTERVAL '1 day',   NULL),                        -- An: Quest 3 mới bắt đầu
(4, 10, 1, 'COMPLETED',   NOW() - INTERVAL '5 days',  NOW() - INTERVAL '1 day'),    -- Dung: Quest 1 hoàn thành
(5, 9,  5, 'EXPIRED',     NOW() - INTERVAL '55 days', NULL),                        -- Cường: Quest 5 hết hạn
(6, 11, 4, 'STOPPED',     NOW() - INTERVAL '4 days',  NULL);                        -- Hoàng: Quest 4 đã dừng

-- ============================================================
-- 35. USER QUEST TASKS
-- Tracks per-task progress for each UserQuest above
-- ============================================================
INSERT INTO "UserQuestTasks" (
    "UserQuestTaskId", "UserQuestId", "QuestTaskId",
    "CurrentValue", "IsCompleted", "CompletedAt", "RewardClaimed"
) VALUES
-- UserQuest 1 (An, Quest 1 IN_PROGRESS):
--   Task CREATE_GHOST_PIN 3/3 done + reward claimed
--   Task REVIEW 1/2 in progress
--   Task ORDER_AMOUNT 95k/200k in progress
(1,  1, 1, 3,      true,  NOW() - INTERVAL '2 days', true),
(2,  1, 2, 1,      false, NULL,                      false),
(3,  1, 3, 95000,  false, NULL,                      false),

-- UserQuest 2 (Binh, Quest 2 COMPLETED):
--   Both tasks done + rewards claimed
(4,  2, 4, 3,      true,  NOW() - INTERVAL '5 days', true),
(5,  2, 5, 105000, true,  NOW() - INTERVAL '2 days', true),

-- UserQuest 3 (An, Quest 3 IN_PROGRESS — fresh start, 0 progress):
(6,  3, 6, 0,      false, NULL,                      false),
(7,  3, 7, 0,      false, NULL,                      false),

-- UserQuest 4 (Dung, Quest 1 COMPLETED):
--   All 3 tasks done + rewards claimed
(8,  4, 1, 3,      true,  NOW() - INTERVAL '4 days', true),
(9,  4, 2, 2,      true,  NOW() - INTERVAL '3 days', true),
(10, 4, 3, 220000, true,  NOW() - INTERVAL '1 day',  true),

-- UserQuest 5 (Cường, Quest 5 EXPIRED — partial, no rewards):
(11, 5, 9, 2,      false, NULL,                      false),
(12, 5, 10, 100000, false, NULL,                     false),

-- UserQuest 6 (Hoàng, Quest 4 STOPPED — started but gave up):
--   Task ORDER_AMOUNT 50k/300k — abandoned, no reward
(13, 6, 8, 50000, false, NULL,                       false);

-- ============================================================
-- 36. RESET IDENTITY SEQUENCES
-- ============================================================
ALTER TABLE "Categories"              ALTER COLUMN "CategoryId"              RESTART WITH 11;
ALTER TABLE "Tastes"                  ALTER COLUMN "TasteId"                 RESTART WITH 6;
ALTER TABLE "Badges"                  ALTER COLUMN "BadgeId"                 RESTART WITH 6;
ALTER TABLE "FeedbackTags"            ALTER COLUMN "TagId"                   RESTART WITH 6;
ALTER TABLE "Users"                   ALTER COLUMN "Id"                      RESTART WITH 12;
ALTER TABLE "UserDietaryPreferences"  ALTER COLUMN "userDietaryPreferencesId" RESTART WITH 7;
ALTER TABLE "Vendors"                 ALTER COLUMN "VendorId"                RESTART WITH 4;
ALTER TABLE "Branches"                ALTER COLUMN "BranchId"                RESTART WITH 6;
ALTER TABLE "BranchImages"            ALTER COLUMN "BranchImageId"           RESTART WITH 8;
ALTER TABLE "WorkSchedules"           ALTER COLUMN "WorkScheduleId"          RESTART WITH 26;
ALTER TABLE "Dishes"                  ALTER COLUMN "DishId"                  RESTART WITH 21;
ALTER TABLE "DishTastes"              ALTER COLUMN "DishTasteId"             RESTART WITH 16;
-- DishDietaryPreferences table dropped – no sequence to reset
ALTER TABLE "VendorDietaryPreferences" ALTER COLUMN "VendorDietaryPreferenceId" RESTART WITH 7;
ALTER TABLE "BranchRequests"          ALTER COLUMN "BranchRequestId"        RESTART WITH 6;
ALTER TABLE "Campaigns"               ALTER COLUMN "CampaignId"              RESTART WITH 6;
ALTER TABLE "BranchCampaigns"         ALTER COLUMN "Id"                      RESTART WITH 8;
ALTER TABLE "Vouchers"                ALTER COLUMN "VoucherId"               RESTART WITH 7;
ALTER TABLE "UserVouchers"            ALTER COLUMN "UserVoucherId"           RESTART WITH 12;
ALTER TABLE "Orders"                  ALTER COLUMN "OrderId"                 RESTART WITH 6;
ALTER TABLE "Feedbacks"               ALTER COLUMN "FeedbackId"              RESTART WITH 6;
ALTER TABLE "FeedbackImages"          ALTER COLUMN "FeedbackImageId"         RESTART WITH 5;
ALTER TABLE "FeedbackTagAssociations" ALTER COLUMN "FeedbackTagId"           RESTART WITH 10;
ALTER TABLE "FeedbackVotes"           ALTER COLUMN "FeedbackVoteId"          RESTART WITH 7;
ALTER TABLE "VendorReplies"           ALTER COLUMN "VendorReplyId"           RESTART WITH 3;
ALTER TABLE "Notifications"           ALTER COLUMN "NotificationId"          RESTART WITH 8;
ALTER TABLE "Quests"                  ALTER COLUMN "QuestId"                 RESTART WITH 6;
ALTER TABLE "QuestTasks"              ALTER COLUMN "QuestTaskId"             RESTART WITH 11;
ALTER TABLE "UserQuests"              ALTER COLUMN "UserQuestId"             RESTART WITH 7;
ALTER TABLE "UserQuestTasks"          ALTER COLUMN "UserQuestTaskId"         RESTART WITH 14;

COMMIT;
