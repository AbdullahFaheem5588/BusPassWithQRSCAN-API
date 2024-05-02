create database BusPassWithQRScan
id int not null identity(1,1) primary key,
name varchar(100),
contact varchar(80),
user_id int,
Foreign key(user_id) references Users(id),
)
id int not null identity(1,1) primary key,
date date,
time time,
userrole varchar(50),
type varchar(100),
description varchar(max),
user_id int ,
foreign key(user_id) references Users(id)
)
id int not null identity(1,1) primary key,
name varchar(150),
latitude varchar(max),
longitude varchar(max),
)
id int not null identity(1,1) primary key,
stop_id int,
foreign key(stop_id) references Stops(id),
student_id int,
foreign key(student_id) references Student(id),
)

create table IsAssigned(
id int not null identity(1,1) primary key,
bus_id int,
foreign key(bus_id) references Bus(id),
route_id int,
)

create table TracksLocation(
id int not null identity(1,1) primary key,
date date,
time time,
latitude varchar(max),
longitude varchar(max),
bus_id int,
foreign key(bus_id) references Bus(id),
route_id int,
)

create table Starts(
id int not null identity(1,1) primary key,
date date,
time time,
bus_id int,
foreign key(bus_id) references Bus(id),
route_id int,
)

create table Reaches(
id int not null identity(1,1) primary key,
date date,
time time,
bus_id int,
foreign key(bus_id) references Bus(id),
route_id int,
)

create table RouteStop(
id int not null identity(1,1) primary key,
stoptiming time,
route_id int,
)
-------------------------------------------------------------------------------------------------------