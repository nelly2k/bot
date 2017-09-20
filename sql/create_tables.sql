CREATE TABLE Ticker(
	id int not null identity constraint pk_ticker primary key,
	capturingDate datetime not null,
	pair nvarchar(15) not null,
	ask_price decimal,
	ask_whole_volume decimal,
	ask_lot_volume decimal,
	bid_price decimal,
	bid_whole_volume decimal,
	bid_lot_volume decimal,
	last_trade_price decimal,
	last_trade_volume decimal,
	volume_today decimal,
	volume_24_hours decimal,
	number_of_trades_today decimal,
	number_of_trades_24_hours decimal,
	low_today decimal,
	low_24_hours decimal,
	high_today decimal,
	high_24_hours decimal,
	opening_price decimal
)

create table Pairs(
	id int not null identity constraint pk_pairs primary key,
	altname nvarchar(15),
	aclass_base  nvarchar(30),
	base  nvarchar(15),
	aclass_quote nvarchar(30),
    quote nvarchar(15),
    lot nvarchar(15),
    pair_decimals int,
    lot_decimals int,
    lot_multiplier int,
    margin_call int,
    margin_stop int
)

create table Trades(
	id int not null identity constraint pk_trades primary key,
	altname nvarchar(15),
	price decimal,
	volume decimal,
	tradeTime datetime,
	buy_sell char(1),
	market_limit char(1),
	misc NVARCHAR(100)
)


create table trade_id(
	trade_id nvarchar(55)
)