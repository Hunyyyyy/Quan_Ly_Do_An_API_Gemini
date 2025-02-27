SELECT 
    u.FullName AS StudentFullName,           -- Họ tên sinh viên
    u.StudentCode,                           -- Mã sinh viên
    u.Email AS StudentEmail,                 -- Email sinh viên
    pt.GroupNumber,                          -- Nhóm của sinh viên
    CASE 
        WHEN pt.UserId = p.ProjectLeaderId THEN 'Yes' 
        ELSE 'No' 
    END AS IsLeader,                        -- Kiểm tra nhóm trưởng
    p.ProjectName,                           -- Tên đồ án
    c.ClassName,                             -- Tên lớp
    i.FullName AS InstructorFullName         -- Tên giảng viên
FROM 
    Classes c
JOIN 
    ClassMembers cm ON c.ClassId = cm.ClassId
JOIN 
    Users u ON cm.UserId = u.UserId
LEFT JOIN 
    ProjectTeams pt ON cm.UserId = pt.UserId
LEFT JOIN 
    Projects p ON pt.ProjectId = p.ProjectId
JOIN 
    Users i ON c.InstructorId = i.UserId   -- Lấy thông tin giảng viên
WHERE 
    c.ClassId IN (31)                  -- Lọc theo các lớp (ví dụ lớp 19 và 20)
ORDER BY 
    c.ClassId, pt.GroupNumber;             -- Sắp xếp theo lớp và nhóm
