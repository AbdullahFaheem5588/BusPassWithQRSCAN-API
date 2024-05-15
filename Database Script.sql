create database BusPassWithQRScanuse BusPassWithQRScancreate table Users(id int not null identity(1,1) Primary key,username varchar(100),password varchar(100),role varchar(50))create table Parent(id int not null identity(1,1) primary key,name varchar(100),contact varchar(80),childrenenroll int,user_id int,Foreign key(user_id) references Users(id))create table Pass(id int not null identity(1,1) Primary key,status varchar(10),passexpiry date,totaljourneys int,remainingjourneys int,)create table Student(id int not null identity(1,1) primary key,name varchar(100),gender varchar(50),regno varchar(100),contact varchar(80),parent_id int,user_id int,pass_id int,Foreign key(user_id) references Users(id),Foreign key(parent_id) references Parent(id),Foreign key(pass_id) references Pass(id))create table Conductor(
id int not null identity(1,1) primary key,
name varchar(100),
contact varchar(80),
user_id int,
Foreign key(user_id) references Users(id),
)create table Admin(id int not null identity(1,1) Primary key,name varchar(100),contact varchar(80),gender varchar(60),user_id int,Foreign key(user_id) references Users(id))create table Notifications(
id int not null identity(1,1) primary key,
date date,
time time,
type varchar(100),
description varchar(max),
notificationRead int,
user_id int ,
foreign key(user_id) references Users(id)
)create table Stops(
id int not null identity(1,1) primary key,
name varchar(150),
latitude varchar(max),
longitude varchar(max),
)create table FavouriteStops(
id int not null identity(1,1) primary key,
stop_id int,
foreign key(stop_id) references Stops(id),
student_id int,
foreign key(student_id) references Student(id),
)create table Bus(id int not null identity(1,1) Primary key,regno varchar(70),totalSeats int,conductor_id int,foreign key(conductor_id) references Conductor(id))create table Route(id int not null identity(1,1) Primary key,Title Varchar(100),)create table Travels(id int not null identity(1,1) Primary key,date date,time time,type varchar(100),pass_id int,foreign key(pass_id) references Pass(id),student_id int,foreign key(student_id) references Student(id),bus_id int,foreign key(bus_id) references Bus(id),route_id int,foreign key(route_id) references Route(id),stop_id int,foreign key(stop_id) references Stops(id))

create table IsAssigned(
id int not null identity(1,1) primary key,
bus_id int,
foreign key(bus_id) references Bus(id),
route_id int,foreign key(route_id) references Route(id),
)

create table TracksLocation(
id int not null identity(1,1) primary key,
date date,
time time,
latitude varchar(max),
longitude varchar(max),
bus_id int,
foreign key(bus_id) references Bus(id),
route_id int,foreign key(route_id) references Route(id),
)

create table Starts(
id int not null identity(1,1) primary key,
date date,
time time,
bus_id int,
foreign key(bus_id) references Bus(id),
route_id int,foreign key(route_id) references Route(id),
)

create table Reaches(
id int not null identity(1,1) primary key,
date date,
time time,
bus_id int,
foreign key(bus_id) references Bus(id),
route_id int,foreign key(route_id) references Route(id),stop_id int,foreign key(stop_id) references Stops(id)
)

create table RouteStop(
id int not null identity(1,1) primary key,
stoptiming time,
route_id int,foreign key(route_id) references Route(id),stop_id int,foreign key(stop_id) references Stops(id)
)
-------------------------------------------------------------------------------------------------------

Select * from Admin
Select * from Bus
Select * from Conductor
Select * from FavouriteStops
Select * from IsAssigned
Select * from Notifications
Select * from Parent
Select * from Pass
Select * from Reaches
Select * from Route
Select * from RouteStop
Select * from Starts
Select * from Stops
Select * from Student
Select * from TracksLocation
Select * from Travels
Select * from Users


Select * from Notifications

update Notifications set user_id = 1