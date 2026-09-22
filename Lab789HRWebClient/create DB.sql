USE SaleDB;
GO

UPDATE Product
SET ImageURL = '/images/dell-16-plus-db16250-ultra-7-256v-x65nw7-1-639039003622656368.jpg'
WHERE Id IN (1, 4);

UPDATE Product
SET ImageURL = '/images/60851_dien_thoai_apple_iphone_17_pro_max_cam_vu_tru_4.jpg'
WHERE Id IN (2, 5);

UPDATE Product
SET ImageURL = '/images/712zVSndv6L._AC_UF894,1000_QL80_.jpg'
WHERE Id IN (3, 6);
GO

-- Tắt Identity Cache cho HRDB
USE HRDB;
GO
ALTER DATABASE SCOPED CONFIGURATION SET IDENTITY_CACHE = OFF;
GO

-- Tắt Identity Cache cho SaleDB
USE SaleDB;
GO
ALTER DATABASE SCOPED CONFIGURATION SET IDENTITY_CACHE = OFF;
GO