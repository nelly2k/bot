
create database bot;
--go
use bot;

--create table Pairs(
--	id int not null identity constraint pk_pairs primary key,
--	altname nvarchar(15),
--	aclass_base  nvarchar(30),
--	base  nvarchar(15),
--	aclass_quote nvarchar(30),
--    quote nvarchar(15),
--    lot nvarchar(15),
--    pair_decimals int,
--    lot_decimals int,
--    lot_multiplier int,
--    margin_call int,
--    margin_stop int
--)

create table trades(
	id int not null identity constraint pk_trades primary key,
	altname nvarchar(15),
	price decimal(18,10),
	volume decimal(18,10),
	tradeTime datetime,
	buy_sell char(1),
	market_limit char(1),
	misc NVARCHAR(100)
)


create index tradeDateTimeIndex on trades (tradeTime)

create table config(
	platform nvarchar(50),
	pair nvarchar(50),
	name nvarchar(50),
	value nvarchar(100),
	constraint pk_config primary key (platform, pair, name)
)

create table log(
	platform nvarchar(50),
	datetime datetime not null constraint df_log default (getdate()),
	status nvarchar(15) not null,
	event nvarchar(max)
)

create table lastEvent(
	platform nvarchar(50),
	name nvarchar(150) not null,
	datetime datetime,
	value nvarchar(MAX)
)
go


create table balance(
	platform nvarchar(50),
	name nvarchar(15),
	volume decimal(18,10),
	price decimal(18,10), 
	notSoldCounter int  constraint df_notSoldCounter default (0),
	notSoldDate DateTime,
	boughtDate Datetime constraint df_boughtDate default (getdate()),
	isBorrowed bit constraint df_balance_isBorrowed default (0) not null,
	isDeleted bit constraint df_balance_isDelete default (0)
)

create table openOrder(
	
	platform nvarchar(50),
	altname nvarchar(15),
	id nvarchar(500),
	isDeleted bit constraint df_openOrder_isDelete default (0)
)

create table operation(
	id int not null primary key identity,
	platform  nvarchar(50),
	isDeleted bit constraint dg_operation_isDelted default (0)
)