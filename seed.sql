PRAGMA foreign_keys = ON;

INSERT OR IGNORE INTO roles (id, name, description) VALUES
('11111111-1111-1111-1111-111111111111', 'Admin', 'Full clinic administration access'),
('22222222-2222-2222-2222-222222222222', 'Doctor', 'Dentist workflow access'),
('33333333-3333-3333-3333-333333333333', 'Receptionist', 'Reception, appointments and billing access');

INSERT OR IGNORE INTO departments (id, name, description, location) VALUES
('aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Stomatologji', 'Sherbime stomatologjike dhe estetike dentare', 'Kati 1'),
('bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Radiologji dentare', 'Kontrolle radiologjike dhe fotografime dentare', 'Kati 1');

INSERT OR IGNORE INTO users (
    id, role_id, username, email, password_hash, first_name, last_name, status, doctor_id
) VALUES
('44444444-4444-4444-4444-444444444444', '11111111-1111-1111-1111-111111111111', 'admin@vleradent.com', 'admin@vleradent.com', 'sha256:3eb3fe66b31e3b4d10fa70b5cad49c7112294af6ae4e476a1c405155d45aa121', 'Vlorentina', 'Sahiti', 'Active', 'dddddddd-dddd-dddd-dddd-dddddddddddd'),
('55555555-5555-5555-5555-555555555555', '22222222-2222-2222-2222-222222222222', 'vlorentina.sahiti@vleradent.com', 'vlorentina.sahiti@vleradent.com', 'sha256:81e22496bd87e5ffae5f2c933154e65c1f290a6cc921072c4f5d3fd08d9b9a87', 'Vlorentina', 'Sahiti', 'Active', 'dddddddd-dddd-dddd-dddd-dddddddddddd'),
('66666666-6666-6666-6666-666666666666', '33333333-3333-3333-3333-333333333333', 'reception@vleradent.com', 'reception@vleradent.com', 'sha256:1b0d33348e535a7cf9b39bd45bd8b7f577a45f7289a554d7d92188e72add5c90', 'Reception', 'Vlera Dent', 'Active', NULL);

INSERT OR IGNORE INTO doctors (
    id, user_id, department_id, first_name, last_name, specialization, phone, email, license_number, working_schedule, status
) VALUES (
    'dddddddd-dddd-dddd-dddd-dddddddddddd',
    '55555555-5555-5555-5555-555555555555',
    'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
    'Vlorentina',
    'Sahiti',
    'Stomatologe',
    '+38344111222',
    'vlorentina.sahiti@vleradent.com',
    'VD-001',
    'Hene - Shtune, 09:00 - 17:00',
    'Active'
);

INSERT OR IGNORE INTO services (id, department_id, service_name, description, price, status) VALUES
('70000000-0000-0000-0000-000000000001', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Kontrolle stomatologjike', 'Kontroll fillestar dhe plan trajtimi', 20, 'Active'),
('70000000-0000-0000-0000-000000000002', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Pastrimi i gurit te dhembeve', 'Pastrimi profesional ultrasonik', 35, 'Active'),
('70000000-0000-0000-0000-000000000003', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Mbushje dhembi', 'Restaurim kompozit estetik', 45, 'Active'),
('70000000-0000-0000-0000-000000000004', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Heqje dhembi', 'Ekstraksion i thjeshte', 40, 'Active'),
('70000000-0000-0000-0000-000000000005', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Zbardhje dhembeve', 'Zbardhje profesionale ne ordinance', 120, 'Active'),
('70000000-0000-0000-0000-000000000006', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Fasadete dentare', 'Fasadete estetike per buzeqeshje', 180, 'Active'),
('70000000-0000-0000-0000-000000000007', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Kurora zirkoni', 'Kurora estetike zirkoni', 160, 'Active'),
('70000000-0000-0000-0000-000000000008', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Trajtim kanali', 'Endodonci per dhemb', 80, 'Active'),
('70000000-0000-0000-0000-000000000009', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'Implant dentar', 'Vendosje implanti dentar', 450, 'Active'),
('70000000-0000-0000-0000-000000000010', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'Rentgen dentar', 'Fotografim diagnostik dentar', 15, 'Active');

INSERT OR IGNORE INTO rooms (id, department_id, room_number, floor, room_type, capacity, current_occupancy, status, price_per_day) VALUES
('80000000-0000-0000-0000-000000000001', 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa', 'ORD-1', '1', 'Dental', 1, 0, 'Available', 0),
('80000000-0000-0000-0000-000000000002', 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb', 'RX-1', '1', 'Radiology', 1, 0, 'Available', 0);

INSERT OR IGNORE INTO beds (id, room_id, bed_number, status) VALUES
('90000000-0000-0000-0000-000000000001', '80000000-0000-0000-0000-000000000001', 'Karrige-1', 'Available'),
('90000000-0000-0000-0000-000000000002', '80000000-0000-0000-0000-000000000002', 'RX-1', 'Available');
