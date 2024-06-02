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

CREATE TRIGGER trg_BusReachedStop
ON Reaches
AFTER INSERT
AS
BEGIN
    DECLARE @busId INT, @stopId INT, @routeId INT, @stopName NVARCHAR(255), @notificationDescription NVARCHAR(255);


    SELECT @busId = inserted.bus_id, @stopId = inserted.stop_id, @routeId = inserted.route_id
    FROM inserted;

    
	IF @stopId IS NOT NULL
    BEGIN
        
		SELECT @stopName = name FROM Stops WHERE id = @stopId;

        
		SET @notificationDescription = 'Bus No ' + CAST(@busId AS NVARCHAR(50)) + ' has reached at your Favourite Stop: ' + @stopName;

        
		INSERT INTO Notifications (user_id, type, description, date, time, notificationRead)
        SELECT u.id, 'Bus Arrived!', @notificationDescription, GETDATE(), CAST(GETDATE() AS TIME), 0
        FROM Users u
        JOIN Student s ON u.id = s.user_id
        JOIN FavouriteStops f ON s.id = f.student_id
        WHERE f.stop_id = @stopId
        AND NOT EXISTS (
            SELECT 1 FROM Notifications n 
            WHERE n.user_id = u.id 
              AND n.description = @notificationDescription 
              AND n.date = CONVERT(DATE, GETDATE())
        );
    END
END;


-------------------------------------------------------------------------------------------------------------------------------------------------------------------

--Rough Work

Select * from Users
Select * from Student
Select * from Pass
Select * from Admin
Select * from Conductor
Select * from Parent
Select * from Notifications
Select * from FavouriteStops
Select * from Bus
Select * from IsAssigned
Select * from Reaches
Select * from Route
Select * from RouteStop
Select * from Starts
Select * from Stops
Select * from Travels
Select * from TracksLocation

Insert into Travels Values(GETDATE(), '07:46:00','pickup_checkin', 5,5,2,2,5)

Update Reaches set stop_id = 2 where id = 12

update RouteStop set stop_id = 2 where id = 12

Update Starts set date = GETDATE()

Insert into Notifications Values('2024-05-20','08:00:00.0000000','Check In!','Checked into Bus',1,0)

SELECT COUNT(*) FROM Reaches WHERE date = '2024-05-25' AND bus_id = 1 AND time >= '04:30:31' AND stop_id IS NULL AND route_id = 1;

Select Top(1) s.route_id from Starts s inner join Bus b on b.id = s.bus_id where s.date = '2024-05-25' AND b.conductor_id = 1 order by s.time desc

delete from Reaches where id > 90
delete from Notifications where id > 5268
delete from TracksLocation where id > 320
delete from Starts where id > 14
delete from Travels where id > 9
delete from Notifications where id > 5268

SELECT u.id
FROM Users u
JOIN Student s ON u.id = s.user_id
JOIN FavouriteStops f ON s.id = f.student_id
WHERE f.stop_id = 1;


Select s.* from Stops s inner join RouteStop rs on s.id = rs.stop_id where rs.route_id = 1