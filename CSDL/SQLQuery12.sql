SELECT StudentCode, COUNT(*) as Count
FROM Users
GROUP BY StudentCode
HAVING COUNT(*) > 1;
select * from users
select * from Classes
INSERT INTO Users (Email, Role, StudentCode, FullName)
VALUES 
('27211202325@dtu.edu.vn', 'Student', '27211202325', 'Ngô Nhật A'),
('27211202326@dtu.edu.vn', 'Student', '27211202326', 'Nguyễn Văn B'),
('27211202327@dtu.edu.vn', 'Student', '27211202327', 'Lê Thị C'),
('27211202328@dtu.edu.vn', 'Student', '27211202328', 'Phạm Minh D'),
('27211202329@dtu.edu.vn', 'Student', '27211202329', 'Trần Hữu E'),
('27211202330@dtu.edu.vn', 'Student', '27211202330', 'Đặng Thị F'),
('27211202331@dtu.edu.vn', 'Student', '27211202331', 'Vũ Văn G'),
('27211202332@dtu.edu.vn', 'Student', '27211202332', 'Phan Thanh H'),
('27211202333@dtu.edu.vn', 'Student', '27211202333', 'Nguyễn Minh I'),
('27211202334@dtu.edu.vn', 'Student', '27211202334', 'Hoàng Đức J'),
('27211202335@dtu.edu.vn', 'Student', '27211202335', 'Đinh Công K'),
('27211202336@dtu.edu.vn', 'Student', '27211202336', 'Phạm Quốc L'),
('27211202337@dtu.edu.vn', 'Student', '27211202337', 'Lý Văn M'),
('27211202338@dtu.edu.vn', 'Student', '27211202338', 'Nguyễn Văn N'),
('27211202339@dtu.edu.vn', 'Student', '27211202339', 'Trần Quang O'),
('27211202340@dtu.edu.vn', 'Student', '27211202340', 'Bùi Hữu P'),
('27211202341@dtu.edu.vn', 'Student', '27211202341', 'Lê Thanh Q'),
('27211202342@dtu.edu.vn', 'Student', '27211202342', 'Võ Công R'),
('27211202343@dtu.edu.vn', 'Student', '27211202343', 'Đỗ Đình S'),
('27211202344@dtu.edu.vn', 'Student', '27211202344', 'Lương Văn T'),
('27211202345@dtu.edu.vn', 'Student', '27211202345', 'Nguyễn Thị U'),
('27211202346@dtu.edu.vn', 'Student', '27211202346', 'Trần Văn V'),
('27211202347@dtu.edu.vn', 'Student', '27211202347', 'Lý Thị W'),
('27211202348@dtu.edu.vn', 'Student', '27211202348', 'Phạm Quốc X'),
('27211202349@dtu.edu.vn', 'Student', '27211202349', 'Trịnh Văn Y'),
('27211202350@dtu.edu.vn', 'Student', '27211202350', 'Đỗ Văn Z'),
('27211202351@dtu.edu.vn', 'Student', '27211202351', 'Lê Văn AA'),
('27211202352@dtu.edu.vn', 'Student', '27211202352', 'Nguyễn Thị BB'),
('27211202353@dtu.edu.vn', 'Student', '27211202353', 'Trần Quốc CC'),
('27211202354@dtu.edu.vn', 'Student', '27211202354', 'Phạm Công DD');

-- Thêm giảng viên (giả sử giảng viên này đã tồn tại trong bảng Users và có UserId = 1)
-- Nếu chưa có, bạn cần thêm giảng viên vào bảng Users trước khi thêm lớp.
INSERT INTO Classes (ClassName, SubjectCode, InstructorId)
VALUES ('CS420A', 'CS420', 1), 
       ('CS440A', 'CS440', 1);
INSERT INTO ClassMembers (ClassId, UserId)
SELECT 30, UserId FROM Users WHERE StudentCode IN 
('27211202311', '27211202322', '27211202333', '27211202344', 
 '27211202355', '27211202366', '27211202377', '27211202388');
INSERT INTO ClassMembers (ClassId, UserId)
SELECT 31, UserId FROM Users WHERE StudentCode IN 
('27211202313', '27211202325', '27211202333', '27211202342', 
 '27211202355', '27211202310', '27211202311', '27211202312', '27211202313');
-- Thêm dự án vào bảng Projects cho lớp CS420A
INSERT INTO Projects (ProjectName, ClassId, ProjectLeaderId)
SELECT N'Dự án 1', 30, UserId FROM Users WHERE StudentCode = '27211202311';
-- Lấy ProjectId của dự án vừa thêm
DECLARE @ProjectId INT;
SELECT @ProjectId = ProjectId FROM Projects WHERE ProjectName = N'Dự án 1' AND ClassId = 30;

-- Thêm các thành viên vào ProjectTeams
INSERT INTO ProjectTeams (ProjectId, UserId)
SELECT 28, UserId FROM Users WHERE StudentCode IN 
('27211202311', '27211202322', '27211202333', '27211202344', 
 '27211202355', '27211202366', '27211202377', '27211202388');
