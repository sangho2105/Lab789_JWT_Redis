create table Product
(
	Id int identity primary key,
	ProductName varchar(100) not null,
	Category varchar(50) not null,
	Price decimal(18, 2) not null,
	Quantity int not null,
	ImageURL varchar(255) null,

	constraint product_price CHECK (Price > 0),
	constraint product_quantity CHECK (Quantity >= 0)
)
go
create table Orders
(
	Id int identity primary key,
	ProductId int not null,
	OrderDate datetime not null,
	Quantity int not null,

	constraint fk_Or_Pro FOREIGN KEY (ProductId) REFERENCES Product(Id),
	constraint order_quantity CHECK (Quantity > 0)
)

select * from Product
select * from Orders

go
insert into Product (ProductName, Category, Price, Quantity, ImageURL) values
('Dell 16 Plus', 'Laptop', 1200, 10, 'images/dell-16-plus-db16250-ultra-7-256v-x65nw7-1-639039003622656368.jpg'),
('Iphone 17 Pro Max', 'Phone', 800.00, 20, 'images/60851_dien_thoai_apple_iphone_17_pro_max_cam_vu_tru_4.jpg'),
('Aula F75 Max', 'Keyboard', 150.00, 30, 'images/712zVSndv6L._AC_UF894,1000_QL80_.jpg')
go
insert into Orders (ProductId, OrderDate, Quantity) values
(1, '2026-09-01', 2),
(2, '2026-09-05', 1),
(3, '2026-09-06', 5)