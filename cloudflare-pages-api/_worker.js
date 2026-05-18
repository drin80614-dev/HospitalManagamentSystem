const corsHeaders = {
  "Access-Control-Allow-Origin": "*",
  "Access-Control-Allow-Methods": "GET,POST,PATCH,OPTIONS",
  "Access-Control-Allow-Headers": "Content-Type, Authorization, X-User-Id",
};

export default {
  async fetch(request, env) {
    if (request.method === "OPTIONS") {
      return new Response(null, { status: 204, headers: corsHeaders });
    }

    try {
      if (!env.DB) {
        return fail("Cloudflare D1 binding DB is not configured.", 500);
      }

      const url = new URL(request.url);
      const path = url.pathname.replace(/\/+$/, "") || "/";
      const parts = path.split("/").filter(Boolean);

      if (parts[0] !== "api") {
        return fail("Not found", 404);
      }

      if (path !== "/api/auth/login" && env.D1_API_TOKEN && request.headers.get("Authorization") !== `Bearer ${env.D1_API_TOKEN}`) {
        return fail("Unauthorized", 401);
      }

      if (request.method === "POST" && path === "/api/auth/login") {
        return login(request, env);
      }

      if (request.method === "GET" && path === "/api/roles") {
        return ok(await all(env.DB.prepare("SELECT * FROM roles ORDER BY name;")));
      }

      if (request.method === "GET" && path === "/api/doctors") {
        return ok(await getDoctors(env, url));
      }

      if (request.method === "GET" && path === "/api/services") {
        return ok(await getServices(env));
      }

      if (request.method === "GET" && path === "/api/departments") {
        return ok(await all(env.DB.prepare("SELECT * FROM departments ORDER BY name;")));
      }

      if (request.method === "GET" && path === "/api/patients/search") {
        return ok(await searchPatients(env, url.searchParams.get("q") ?? ""));
      }

      if (request.method === "GET" && path === "/api/patients") {
        return ok(await searchPatients(env, url.searchParams.get("q") ?? ""));
      }

      if (request.method === "POST" && path === "/api/patients") {
        return createPatient(request, env);
      }

      if (parts[1] === "patients" && parts[2] && !parts[3] && request.method === "GET") {
        const patient = await getPatient(env, parts[2]);
        return patient ? ok(patient) : fail("Patient not found", 404);
      }

      if (parts[1] === "patients" && parts[2] && parts[3] === "history" && request.method === "GET") {
        return ok(await getPatientHistory(env, parts[2]));
      }

      if (request.method === "GET" && path === "/api/appointments") {
        return ok(await getAppointments(env, url));
      }

      if (request.method === "POST" && path === "/api/appointments") {
        return createAppointment(request, env);
      }

      if (parts[1] === "appointments" && parts[2] && request.method === "GET") {
        const appointment = await getAppointment(env, parts[2]);
        return appointment ? ok(appointment) : fail("Appointment not found", 404);
      }

      if (parts[1] === "appointments" && parts[2] && request.method === "PATCH") {
        return updateAppointment(request, env, parts[2]);
      }

      if (request.method === "POST" && path === "/api/visits") {
        return createVisit(request, env);
      }

      if (request.method === "POST" && path === "/api/prescriptions") {
        return createPrescription(request, env);
      }

      if (request.method === "GET" && path === "/api/prescriptions/pending-print") {
        return ok(await getPendingPrescriptions(env));
      }

      if (parts[1] === "prescriptions" && parts[2] && !parts[3] && request.method === "GET") {
        const prescription = await getPrescription(env, parts[2]);
        return prescription ? ok(prescription) : fail("Prescription not found", 404);
      }

      if (parts[1] === "prescriptions" && parts[2] && parts[3] === "printed" && request.method === "PATCH") {
        return markPrinted(request, env, "prescriptions", parts[2]);
      }

      if (request.method === "POST" && path === "/api/payments") {
        return createPayment(request, env);
      }

      if (request.method === "GET" && path === "/api/payments") {
        return ok(await getPayments(env, url));
      }

      if (request.method === "GET" && path === "/api/payments/pending-print") {
        return ok(await getPendingPayments(env));
      }

      if (parts[1] === "payments" && parts[2] && !parts[3] && request.method === "GET") {
        const payment = await getPayment(env, parts[2]);
        return payment ? ok(payment) : fail("Payment not found", 404);
      }

      if (parts[1] === "payments" && parts[2] && parts[3] === "balance" && request.method === "PATCH") {
        return updatePaymentBalance(request, env, parts[2]);
      }

      if (parts[1] === "payments" && parts[2] && parts[3] === "printed" && request.method === "PATCH") {
        return markPrinted(request, env, "payments", parts[2]);
      }

      if (parts[1] === "invoices" && parts[2] === "by-payment" && parts[3] && request.method === "GET") {
        const invoice = await getInvoiceByPayment(env, parts[3]);
        return invoice ? ok(invoice) : fail("Invoice not found", 404);
      }

      if (request.method === "GET" && path === "/api/rooms") {
        return ok(await getRooms(env));
      }

      if (request.method === "POST" && path === "/api/hospitalizations") {
        return createHospitalization(request, env);
      }

      if (request.method === "GET" && path === "/api/reports/daily") {
        return ok(await getDailyReport(env, url));
      }

      return fail("Not found", 404);
    } catch (error) {
      console.error(error);
      return fail("Unexpected server error", 500, { detail: error.message });
    }
  },
};

function ok(data, status = 200) {
  return new Response(JSON.stringify(data), {
    status,
    headers: { ...corsHeaders, "Content-Type": "application/json; charset=utf-8" },
  });
}

function fail(message, status = 400, extra = {}) {
  return ok({ error: message, ...extra }, status);
}

async function bodyJson(request) {
  try {
    return await request.json();
  } catch {
    return {};
  }
}

function read(obj, ...names) {
  for (const name of names) {
    if (obj && obj[name] !== undefined && obj[name] !== null) {
      return obj[name];
    }
  }

  return undefined;
}

function required(value, name) {
  if (value === undefined || value === null || String(value).trim() === "") {
    throw new Error(`${name} is required.`);
  }

  return value;
}

function id() {
  return crypto.randomUUID();
}

function now() {
  return new Date().toISOString();
}

function dateOnly(value) {
  if (!value) {
    return new Date().toISOString().slice(0, 10);
  }

  return String(value).slice(0, 10);
}

function timeOnly(value) {
  const raw = String(value || "09:00:00");
  return raw.length === 5 ? `${raw}:00` : raw;
}

function toCamel(row) {
  const converted = {};
  for (const [key, value] of Object.entries(row || {})) {
    const camel = key.replace(/_([a-z])/g, (_, letter) => letter.toUpperCase());
    converted[camel] = isDateLikeKey(key) ? toIsoTimestamp(value) : value;
  }

  return converted;
}

function isDateLikeKey(key) {
  return key.endsWith("_at") || key.endsWith("_date") || key === "date" || key === "payment_date" || key === "visit_date";
}

function toIsoTimestamp(value) {
  if (!value || typeof value !== "string") {
    return value;
  }

  if (value.includes("T")) {
    return value.endsWith("Z") || /[+-]\d\d:\d\d$/.test(value) ? value : `${value}Z`;
  }

  return value.length === 10 ? `${value}T00:00:00Z` : `${value.replace(" ", "T")}Z`;
}

async function all(statement) {
  const result = await statement.all();
  return (result.results || []).map(toCamel);
}

async function first(statement) {
  const result = await statement.first();
  return result ? toCamel(result) : null;
}

async function audit(env, request, action, entityName, entityId, details) {
  const actor = request?.headers?.get("X-User-Id") || null;
  const ip = request?.headers?.get("CF-Connecting-IP") || null;
  await env.DB.prepare(
    "INSERT INTO audit_logs (id, user_id, action, entity_name, entity_id, details, ip_address) VALUES (?, ?, ?, ?, ?, ?, ?);",
  )
    .bind(id(), actor, action, entityName, entityId, details ?? null, ip)
    .run();
}

async function login(request, env) {
  const body = await bodyJson(request);
  const loginValue = String(required(read(body, "login", "Login"), "login")).trim().toLowerCase();
  const password = String(required(read(body, "password", "Password"), "password"));

  const user = await first(
    env.DB.prepare(`
      SELECT u.*, r.name AS role_name
      FROM users u
      JOIN roles r ON r.id = u.role_id
      WHERE lower(u.username) = ? OR lower(u.email) = ?
      LIMIT 1;
    `).bind(loginValue, loginValue),
  );

  if (!user || user.status !== "Active") {
    return fail("Invalid credentials", 401);
  }

  const verified = await verifyPassword(password, user.passwordHash);
  if (!verified) {
    return fail("Invalid credentials", 401);
  }

  await env.DB.prepare("UPDATE users SET last_login_at = CURRENT_TIMESTAMP, updated_at = CURRENT_TIMESTAMP WHERE id = ?;")
    .bind(user.id)
    .run();

  delete user.passwordHash;
  return ok({ user, sessionId: id() });
}

async function verifyPassword(password, storedHash) {
  if (!storedHash) {
    return false;
  }

  if (storedHash.startsWith("sha256:")) {
    return (await sha256Hex(password)) === storedHash.substring("sha256:".length);
  }

  const parts = storedHash.split("$");
  if (parts.length === 4 && parts[0] === "PBKDF2-SHA256") {
    const iterations = Number(parts[1]);
    const salt = Uint8Array.from(atob(parts[2]), (c) => c.charCodeAt(0));
    const expected = Uint8Array.from(atob(parts[3]), (c) => c.charCodeAt(0));
    const key = await crypto.subtle.importKey("raw", new TextEncoder().encode(password), "PBKDF2", false, ["deriveBits"]);
    const bits = await crypto.subtle.deriveBits(
      { name: "PBKDF2", hash: "SHA-256", salt, iterations },
      key,
      expected.length * 8,
    );
    return fixedEqual(new Uint8Array(bits), expected);
  }

  return false;
}

async function sha256Hex(value) {
  const buffer = await crypto.subtle.digest("SHA-256", new TextEncoder().encode(value));
  return [...new Uint8Array(buffer)].map((byte) => byte.toString(16).padStart(2, "0")).join("");
}

function fixedEqual(a, b) {
  if (a.length !== b.length) {
    return false;
  }

  let diff = 0;
  for (let i = 0; i < a.length; i += 1) {
    diff |= a[i] ^ b[i];
  }

  return diff === 0;
}

async function getDoctors(env, url) {
  const activeOnly = url.searchParams.get("activeOnly") === "true";
  return all(
    env.DB.prepare(`
      SELECT d.*, dep.name AS department_name
      FROM doctors d
      LEFT JOIN departments dep ON dep.id = d.department_id
      WHERE (? = 0 OR d.status = 'Active')
      ORDER BY d.first_name, d.last_name;
    `).bind(activeOnly ? 1 : 0),
  );
}

async function getServices(env) {
  return all(
    env.DB.prepare(`
      SELECT s.*, d.name AS department_name
      FROM services s
      LEFT JOIN departments d ON d.id = s.department_id
      WHERE s.status = 'Active'
      ORDER BY s.service_name;
    `),
  );
}

async function serviceRows(env, serviceIds) {
  const ids = [...new Set((serviceIds || []).filter(Boolean))];
  if (!ids.length) {
    return [];
  }

  const rows = [];
  for (const serviceId of ids) {
    const service = await first(env.DB.prepare("SELECT * FROM services WHERE id = ? AND status = 'Active';").bind(serviceId));
    if (service) {
      rows.push(service);
    }
  }

  if (rows.length !== ids.length) {
    throw new Error("One or more selected services were not found.");
  }

  return rows;
}

async function searchPatients(env, query) {
  const q = `%${String(query || "").trim().toLowerCase()}%`;
  return all(
    env.DB.prepare(`
      SELECT p.*, d.first_name || ' ' || d.last_name AS assigned_doctor_name, r.room_number AS current_room_number
      FROM patients p
      LEFT JOIN doctors d ON d.id = p.assigned_doctor_id
      LEFT JOIN rooms r ON r.id = p.current_room_id
      WHERE ? = '%%'
         OR lower(p.full_name) LIKE ?
         OR lower(COALESCE(p.personal_number, '')) LIKE ?
         OR lower(COALESCE(p.phone, '')) LIKE ?
         OR lower(COALESCE(p.hospital_number, '')) LIKE ?
      ORDER BY p.created_at DESC
      LIMIT 50;
    `).bind(q, q, q, q, q),
  );
}

async function getPatient(env, patientId) {
  return first(
    env.DB.prepare(`
      SELECT p.*, d.first_name || ' ' || d.last_name AS assigned_doctor_name, r.room_number AS current_room_number
      FROM patients p
      LEFT JOIN doctors d ON d.id = p.assigned_doctor_id
      LEFT JOIN rooms r ON r.id = p.current_room_id
      WHERE p.id = ?;
    `).bind(patientId),
  );
}

async function createPatient(request, env) {
  const body = await bodyJson(request);
  const firstName = String(required(read(body, "firstName", "FirstName", "first_name"), "firstName")).trim();
  const lastName = String(required(read(body, "lastName", "LastName", "last_name"), "lastName")).trim();
  const phone = String(required(read(body, "phone", "Phone"), "phone")).trim();
  const patientId = id();
  const hospitalNumber = read(body, "hospitalNumber", "HospitalNumber") || `VD-${Date.now().toString().slice(-8)}`;
  const assignedDoctorId = read(body, "assignedDoctorId", "AssignedDoctorId", "doctorId") || null;
  const fullName = `${firstName} ${lastName}`.trim();

  await env.DB.prepare(`
    INSERT INTO patients (
      id, hospital_number, assigned_doctor_id, first_name, last_name, full_name, date_of_birth, gender,
      personal_number, phone, email, address, blood_type, allergies, chronic_diseases, registration_date, status
    )
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
  `)
    .bind(
      patientId,
      hospitalNumber,
      assignedDoctorId,
      firstName,
      lastName,
      fullName,
      read(body, "dateOfBirth", "DateOfBirth") || null,
      read(body, "gender", "Gender") || null,
      read(body, "personalNumber", "PersonalNumber") || null,
      phone,
      read(body, "email", "Email") || null,
      read(body, "address", "Address") || null,
      read(body, "bloodType", "BloodType") || null,
      read(body, "allergies", "Allergies") || null,
      read(body, "chronicDiseases", "ChronicDiseases") || null,
      dateOnly(read(body, "registrationDate", "RegistrationDate")),
      read(body, "status", "Status") || "Active",
    )
    .run();

  await audit(env, request, "patient created", "patients", patientId, fullName);
  return ok(await getPatient(env, patientId), 201);
}

async function getPatientHistory(env, patientId) {
  const patient = await getPatient(env, patientId);
  if (!patient) {
    throw new Error("Patient not found.");
  }

  const visits = await all(
    env.DB.prepare(`
      SELECT mv.*, d.first_name || ' ' || d.last_name AS doctor_name
      FROM medical_visits mv
      LEFT JOIN doctors d ON d.id = mv.doctor_id
      WHERE mv.patient_id = ?
      ORDER BY mv.visit_date DESC;
    `).bind(patientId),
  );

  const diagnoses = await all(
    env.DB.prepare(`
      SELECT dg.*, d.first_name || ' ' || d.last_name AS doctor_name
      FROM diagnoses dg
      LEFT JOIN doctors d ON d.id = dg.doctor_id
      WHERE dg.patient_id = ?
      ORDER BY dg.diagnosis_date DESC;
    `).bind(patientId),
  );

  const prescriptions = await all(
    env.DB.prepare(`
      SELECT pr.*, d.first_name || ' ' || d.last_name AS doctor_name
      FROM prescriptions pr
      LEFT JOIN doctors d ON d.id = pr.doctor_id
      WHERE pr.patient_id = ?
      ORDER BY pr.prescription_date DESC;
    `).bind(patientId),
  );

  const payments = await getPayments(env, new URL(`https://local/api/payments?patientId=${patientId}`));

  const appointments = await all(
    env.DB.prepare(`
      SELECT a.*, group_concat(s.service_name, ', ') AS service_names, SUM(s.price) AS services_total
      FROM appointments a
      LEFT JOIN appointment_services aps ON aps.appointment_id = a.id
      LEFT JOIN services s ON s.id = aps.service_id
      WHERE a.patient_id = ?
      GROUP BY a.id
      ORDER BY a.date DESC, a.time DESC;
    `).bind(patientId),
  );

  return { patient, visits, diagnoses, prescriptions, payments, appointments };
}

async function getAppointments(env, url) {
  const doctorId = url.searchParams.get("doctorId") || "";
  const date = url.searchParams.get("date") || "";
  const status = url.searchParams.get("status") || "";
  return all(
    env.DB.prepare(`
      SELECT a.*,
             a.date AS appointment_date,
             a.time AS appointment_time,
             p.full_name AS patient_name,
             d.first_name || ' ' || d.last_name AS doctor_name,
             d.specialization AS doctor_specialization,
             group_concat(s.service_name, ', ') AS service_names,
             SUM(s.price) AS services_total,
             MIN(s.service_name) AS service_name,
             SUM(s.price) AS service_price
      FROM appointments a
      JOIN patients p ON p.id = a.patient_id
      JOIN doctors d ON d.id = a.doctor_id
      LEFT JOIN appointment_services aps ON aps.appointment_id = a.id
      LEFT JOIN services s ON s.id = aps.service_id
      WHERE (? = '' OR a.doctor_id = ?)
        AND (? = '' OR a.date = ?)
        AND (? = '' OR a.status = ?)
      GROUP BY a.id
      ORDER BY a.date DESC, a.time DESC;
    `).bind(doctorId, doctorId, date, date, status, status),
  );
}

async function getAppointment(env, appointmentId) {
  const rows = await getAppointments(env, new URL(`https://local/api/appointments`));
  return rows.find((row) => row.id === appointmentId) || null;
}

async function createAppointment(request, env) {
  const body = await bodyJson(request);
  const patientId = String(required(read(body, "patientId", "PatientId"), "patientId"));
  const doctorId = String(required(read(body, "doctorId", "DoctorId"), "doctorId"));
  const serviceIds = read(body, "serviceIds", "ServiceIds") || [read(body, "serviceId", "ServiceId")].filter(Boolean);
  const services = await serviceRows(env, serviceIds);
  const total = services.reduce((sum, service) => sum + Number(service.price || 0), 0);
  const appointmentId = id();
  const visitId = id();
  const paymentId = id();
  const invoiceId = id();
  const invoiceNumber = `INV-${Date.now().toString().slice(-8)}`;
  const servicesJson = JSON.stringify(services.map((service) => ({ id: service.id, name: service.serviceName, price: service.price })));
  const serviceNames = services.map((service) => service.serviceName).join(", ");
  const reason = String(required(read(body, "reason", "Reason"), "reason"));
  const appointmentDate = dateOnly(read(body, "appointmentDate", "AppointmentDate", "date"));
  const appointmentTime = timeOnly(read(body, "appointmentTime", "AppointmentTime", "time"));
  const appointmentNumber = `APT-${Date.now().toString().slice(-8)}`;

  const statements = [
    env.DB.prepare(`
      INSERT INTO appointments (id, appointment_number, patient_id, doctor_id, service_id, date, time, reason, status, notes, services_json, total_amount)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
    `).bind(
      appointmentId,
      appointmentNumber,
      patientId,
      doctorId,
      services[0]?.id || null,
      appointmentDate,
      appointmentTime,
      reason,
      read(body, "status", "Status") || "Scheduled",
      read(body, "notes", "Notes") || null,
      servicesJson,
      total,
    ),
    env.DB.prepare(`
      INSERT INTO medical_visits (id, patient_id, doctor_id, appointment_id, visit_date, symptoms, diagnosis, disease, treatment_plan, notes, status)
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
    `).bind(
      visitId,
      patientId,
      doctorId,
      appointmentId,
      `${appointmentDate}T${appointmentTime}`,
      reason,
      serviceNames,
      serviceNames,
      `Planifikuar: ${serviceNames}`,
      read(body, "notes", "Notes") || null,
      "Scheduled",
    ),
    env.DB.prepare(`
      INSERT INTO payments (id, patient_id, appointment_id, service_id, total_amount, paid_amount, balance_amount, payment_method, status, notes, services_json)
      VALUES (?, ?, ?, ?, ?, 0, ?, 'Cash', 'Pending', ?, ?);
    `).bind(paymentId, patientId, appointmentId, services[0]?.id || null, total, total, `Automatik nga termini ${appointmentNumber}`, servicesJson),
    env.DB.prepare(`
      INSERT INTO invoices (id, payment_id, invoice_number, total_amount, paid_amount, balance_amount, status)
      VALUES (?, ?, ?, ?, 0, ?, 'Issued');
    `).bind(invoiceId, paymentId, invoiceNumber, total, total),
  ];

  for (const service of services) {
    statements.push(
      env.DB.prepare("INSERT INTO appointment_services (appointment_id, service_id) VALUES (?, ?);").bind(appointmentId, service.id),
    );
    statements.push(env.DB.prepare("INSERT INTO payment_services (payment_id, service_id) VALUES (?, ?);").bind(paymentId, service.id));
  }

  await env.DB.batch(statements);
  await audit(env, request, "appointment created", "appointments", appointmentId, `${reason}; ${serviceNames}`);
  await audit(env, request, "visit created from appointment", "medical_visits", visitId, serviceNames);
  await audit(env, request, "payment created from appointment", "payments", paymentId, `Total ${total}`);
  return ok(await getAppointment(env, appointmentId), 201);
}

async function updateAppointment(request, env, appointmentId) {
  const body = await bodyJson(request);
  const serviceIds = read(body, "serviceIds", "ServiceIds");
  let services = null;
  let total = null;
  let servicesJson = null;
  let serviceNames = null;

  if (Array.isArray(serviceIds) && serviceIds.length > 0) {
    services = await serviceRows(env, serviceIds);
    total = services.reduce((sum, service) => sum + Number(service.price || 0), 0);
    servicesJson = JSON.stringify(services.map((service) => ({ id: service.id, name: service.serviceName, price: service.price })));
    serviceNames = services.map((service) => service.serviceName).join(", ");
  }

  const existing = await first(env.DB.prepare("SELECT * FROM appointments WHERE id = ?;").bind(appointmentId));
  if (!existing) {
    return fail("Appointment not found", 404);
  }

  await env.DB.prepare(`
    UPDATE appointments
    SET date = ?, time = ?, reason = ?, status = ?, notes = ?, service_id = COALESCE(?, service_id),
        services_json = COALESCE(?, services_json), total_amount = COALESCE(?, total_amount), updated_at = CURRENT_TIMESTAMP
    WHERE id = ?;
  `)
    .bind(
      dateOnly(read(body, "appointmentDate", "AppointmentDate", "date") || existing.date),
      timeOnly(read(body, "appointmentTime", "AppointmentTime", "time") || existing.time),
      read(body, "reason", "Reason") || existing.reason,
      read(body, "status", "Status") || existing.status,
      read(body, "notes", "Notes") || existing.notes || null,
      services?.[0]?.id || null,
      servicesJson,
      total,
      appointmentId,
    )
    .run();

  if (services) {
    await env.DB.prepare("DELETE FROM appointment_services WHERE appointment_id = ?;").bind(appointmentId).run();
    for (const service of services) {
      await env.DB.prepare("INSERT INTO appointment_services (appointment_id, service_id) VALUES (?, ?);")
        .bind(appointmentId, service.id)
        .run();
    }

    await env.DB.prepare(`
      UPDATE medical_visits
      SET diagnosis = ?, disease = ?, treatment_plan = ?, updated_at = CURRENT_TIMESTAMP
      WHERE appointment_id = ?;
    `).bind(serviceNames, serviceNames, `Planifikuar: ${serviceNames}`, appointmentId).run();
  }

  await audit(env, request, "appointment updated", "appointments", appointmentId, "Appointment schedule or services changed");
  return ok(await getAppointment(env, appointmentId));
}

async function createVisit(request, env) {
  const body = await bodyJson(request);
  const visitId = id();
  await env.DB.prepare(`
    INSERT INTO medical_visits (
      id, patient_id, doctor_id, appointment_id, room_id, visit_date, symptoms, diagnosis, disease, treatment_plan, notes, follow_up_date, status
    )
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);
  `)
    .bind(
      visitId,
      required(read(body, "patientId", "PatientId"), "patientId"),
      required(read(body, "doctorId", "DoctorId"), "doctorId"),
      read(body, "appointmentId", "AppointmentId") || null,
      read(body, "roomId", "RoomId") || null,
      read(body, "visitDate", "VisitDate") || now(),
      read(body, "symptoms", "Symptoms") || "",
      read(body, "diagnosis", "Diagnosis") || "",
      read(body, "disease", "Disease") || "",
      read(body, "treatmentPlan", "TreatmentPlan") || "",
      read(body, "notes", "Notes") || null,
      read(body, "followUpDate", "FollowUpDate") || null,
      read(body, "status", "VisitStatus") || "Open",
    )
    .run();

  await audit(env, request, "visit created", "medical_visits", visitId, read(body, "diagnosis", "Diagnosis") || "");
  return ok({ id: visitId }, 201);
}

async function createPrescription(request, env) {
  const body = await bodyJson(request);
  const prescriptionId = id();
  const items = Array.isArray(body.items)
    ? body.items
    : [
        {
          medicationName: read(body, "medicationName", "MedicationName"),
          dosage: read(body, "dosage", "Dosage"),
          frequency: read(body, "frequency", "Frequency"),
          duration: read(body, "duration", "Duration"),
          instructions: read(body, "instructions", "Instructions"),
        },
      ];

  await env.DB.prepare(`
    INSERT INTO prescriptions (
      id, patient_id, doctor_id, visit_id, appointment_id, medication_name, dosage, frequency, duration, instructions, prescription_date, status, print_status
    )
    VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'Active', 'Pending');
  `)
    .bind(
      prescriptionId,
      required(read(body, "patientId", "PatientId"), "patientId"),
      required(read(body, "doctorId", "DoctorId"), "doctorId"),
      read(body, "visitId", "VisitId") || null,
      read(body, "appointmentId", "AppointmentId") || null,
      items[0]?.medicationName || items[0]?.MedicationName || null,
      items[0]?.dosage || items[0]?.Dosage || null,
      items[0]?.frequency || items[0]?.Frequency || null,
      items[0]?.duration || items[0]?.Duration || null,
      read(body, "instructions", "Instructions") || items[0]?.instructions || null,
      dateOnly(read(body, "prescriptionDate", "PrescriptionDate")),
    )
    .run();

  for (const item of items) {
    const medicationName = read(item, "medicationName", "MedicationName");
    if (medicationName) {
      await env.DB.prepare(`
        INSERT INTO prescription_items (id, prescription_id, medication_name, dosage, frequency, duration, instructions)
        VALUES (?, ?, ?, ?, ?, ?, ?);
      `)
        .bind(
          id(),
          prescriptionId,
          medicationName,
          read(item, "dosage", "Dosage") || null,
          read(item, "frequency", "Frequency") || null,
          read(item, "duration", "Duration") || null,
          read(item, "instructions", "Instructions") || null,
        )
        .run();
    }
  }

  await audit(env, request, "prescription created", "prescriptions", prescriptionId, items.map((item) => read(item, "medicationName", "MedicationName")).filter(Boolean).join(", "));
  return ok({ id: prescriptionId }, 201);
}

async function getPendingPrescriptions(env) {
  return all(
    env.DB.prepare(`
      SELECT pr.*, p.full_name AS patient_name, d.first_name || ' ' || d.last_name AS doctor_name
      FROM prescriptions pr
      JOIN patients p ON p.id = pr.patient_id
      JOIN doctors d ON d.id = pr.doctor_id
      WHERE pr.print_status = 'Pending'
      ORDER BY pr.created_at ASC;
    `),
  );
}

async function getPrescription(env, prescriptionId) {
  return first(
    env.DB.prepare(`
      SELECT pr.*,
             p.full_name AS patient_name,
             d.first_name || ' ' || d.last_name AS doctor_name,
             d.license_number AS doctor_license_number
      FROM prescriptions pr
      JOIN patients p ON p.id = pr.patient_id
      JOIN doctors d ON d.id = pr.doctor_id
      WHERE pr.id = ?;
    `).bind(prescriptionId),
  );
}

async function createPayment(request, env) {
  const body = await bodyJson(request);
  const serviceIds = read(body, "serviceIds", "ServiceIds") || [read(body, "serviceId", "ServiceId")].filter(Boolean);
  const services = await serviceRows(env, serviceIds);
  const calculatedTotal = services.reduce((sum, service) => sum + Number(service.price || 0), 0);
  const total = Number(read(body, "totalAmount", "TotalAmount") ?? calculatedTotal);
  const paid = Number(read(body, "paidAmount", "PaidAmount", "amount", "Amount") ?? 0);
  if (Number.isNaN(total) || Number.isNaN(paid) || paid < 0 || total < 0 || paid > total) {
    return fail("Invalid payment amounts", 422);
  }

  const paymentId = id();
  const invoiceId = id();
  const balance = Math.max(total - paid, 0);
  const status = read(body, "status", "Status") === "Cancelled" ? "Cancelled" : balance > 0 ? "Pending" : "Paid";
  const servicesJson = JSON.stringify(services.map((service) => ({ id: service.id, name: service.serviceName, price: service.price })));
  const statements = [
    env.DB.prepare(`
      INSERT INTO payments (
        id, patient_id, appointment_id, service_id, total_amount, paid_amount, balance_amount, payment_method, payment_date, status, notes, services_json, print_status
      )
      VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, 'Pending');
    `).bind(
      paymentId,
      required(read(body, "patientId", "PatientId"), "patientId"),
      read(body, "appointmentId", "AppointmentId") || null,
      services[0]?.id || null,
      total,
      paid,
      balance,
      read(body, "paymentMethod", "PaymentMethod") || "Cash",
      read(body, "paymentDate", "PaymentDate") || now(),
      status,
      read(body, "notes", "Notes") || null,
      servicesJson,
    ),
    env.DB.prepare(`
      INSERT INTO invoices (id, payment_id, invoice_number, total_amount, paid_amount, balance_amount, status)
      VALUES (?, ?, ?, ?, ?, ?, 'Issued');
    `).bind(invoiceId, paymentId, `INV-${Date.now().toString().slice(-8)}`, total, paid, balance),
  ];

  for (const service of services) {
    statements.push(env.DB.prepare("INSERT INTO payment_services (payment_id, service_id) VALUES (?, ?);").bind(paymentId, service.id));
  }

  await env.DB.batch(statements);
  await audit(env, request, "payment registered", "payments", paymentId, `Total ${total}, paid ${paid}, balance ${balance}`);
  return ok(await getPayment(env, paymentId), 201);
}

async function getPayments(env, url) {
  const patientId = url.searchParams.get("patientId") || "";
  return all(
    env.DB.prepare(`
      SELECT py.*,
             py.paid_amount AS amount,
             p.full_name AS patient_name,
             p.hospital_number,
             a.appointment_number,
             a.date AS appointment_date,
             a.time AS appointment_time,
             group_concat(s.service_name, ', ') AS service_names,
             MIN(s.service_name) AS service_name
      FROM payments py
      JOIN patients p ON p.id = py.patient_id
      LEFT JOIN appointments a ON a.id = py.appointment_id
      LEFT JOIN payment_services ps ON ps.payment_id = py.id
      LEFT JOIN services s ON s.id = ps.service_id
      WHERE (? = '' OR py.patient_id = ?)
      GROUP BY py.id
      ORDER BY py.payment_date DESC;
    `).bind(patientId, patientId),
  );
}

async function getPayment(env, paymentId) {
  const rows = await getPayments(env, new URL("https://local/api/payments"));
  return rows.find((row) => row.id === paymentId) || null;
}

async function updatePaymentBalance(request, env, paymentId) {
  const body = await bodyJson(request);
  const payment = await first(env.DB.prepare("SELECT * FROM payments WHERE id = ?;").bind(paymentId));
  if (!payment) {
    return fail("Payment not found", 404);
  }

  const balance = Number(required(read(body, "balanceAmount", "BalanceAmount"), "balanceAmount"));
  if (Number.isNaN(balance) || balance < 0 || balance > Number(payment.totalAmount)) {
    return fail("Invalid balance amount", 422);
  }

  const paid = Math.max(Number(payment.totalAmount) - balance, 0);
  const status = read(body, "status", "Status") === "Cancelled" ? "Cancelled" : balance > 0 ? "Pending" : "Paid";

  await env.DB.prepare(`
    UPDATE payments
    SET paid_amount = ?, balance_amount = ?, status = ?, notes = COALESCE(?, notes), updated_at = CURRENT_TIMESTAMP
    WHERE id = ?;
  `).bind(paid, balance, status, read(body, "notes", "Notes") || null, paymentId).run();

  await env.DB.prepare(`
    UPDATE invoices
    SET paid_amount = ?, balance_amount = ?, updated_at = CURRENT_TIMESTAMP
    WHERE payment_id = ?;
  `).bind(paid, balance, paymentId).run();

  await audit(env, request, "payment balance updated", "payments", paymentId, `Paid ${paid}, balance ${balance}`);
  return ok(await getPayment(env, paymentId));
}

async function getPendingPayments(env) {
  return all(
    env.DB.prepare(`
      SELECT py.*, py.paid_amount AS amount, p.full_name AS patient_name, p.hospital_number
      FROM payments py
      JOIN patients p ON p.id = py.patient_id
      WHERE py.print_status = 'Pending'
      ORDER BY py.created_at ASC;
    `),
  );
}

async function markPrinted(request, env, table, recordId) {
  const body = await bodyJson(request);
  await env.DB.prepare(`UPDATE ${table} SET print_status = 'Printed', printed_at = CURRENT_TIMESTAMP, printed_by = ?, updated_at = CURRENT_TIMESTAMP WHERE id = ?;`)
    .bind(read(body, "printedBy", "PrintedBy") || request.headers.get("X-User-Id") || null, recordId)
    .run();

  await audit(env, request, `${table.slice(0, -1)} printed`, table, recordId, "Marked as printed");
  return ok({ id: recordId, printStatus: "Printed" });
}

async function getInvoiceByPayment(env, paymentId) {
  return first(
    env.DB.prepare(`
      SELECT inv.*, p.full_name AS patient_name, p.hospital_number, py.payment_method,
             group_concat(s.service_name, ', ') AS service_name
      FROM invoices inv
      JOIN payments py ON py.id = inv.payment_id
      JOIN patients p ON p.id = py.patient_id
      LEFT JOIN payment_services ps ON ps.payment_id = py.id
      LEFT JOIN services s ON s.id = ps.service_id
      WHERE inv.payment_id = ?
      GROUP BY inv.id;
    `).bind(paymentId),
  );
}

async function getRooms(env) {
  return all(
    env.DB.prepare(`
      SELECT r.*, d.name AS department_name
      FROM rooms r
      LEFT JOIN departments d ON d.id = r.department_id
      ORDER BY r.room_number;
    `),
  );
}

async function createHospitalization(request, env) {
  const body = await bodyJson(request);
  const hospitalizationId = id();
  const patientId = required(read(body, "patientId", "PatientId"), "patientId");
  const roomId = required(read(body, "roomId", "RoomId"), "roomId");
  const bedId = read(body, "bedId", "BedId") || null;

  const room = await first(env.DB.prepare("SELECT * FROM rooms WHERE id = ?;").bind(roomId));
  if (!room) {
    return fail("Room not found", 404);
  }

  if (Number(room.currentOccupancy) >= Number(room.capacity)) {
    return fail("Room capacity is full", 409);
  }

  const statements = [
    env.DB.prepare(`
      INSERT INTO hospitalizations (id, patient_id, room_id, bed_id, admission_date, expected_discharge_date, notes, status)
      VALUES (?, ?, ?, ?, ?, ?, ?, 'Active');
    `).bind(
      hospitalizationId,
      patientId,
      roomId,
      bedId,
      read(body, "admissionDate", "AdmissionDate") || now(),
      read(body, "expectedDischargeDate", "ExpectedDischargeDate") || null,
      read(body, "notes", "Notes") || null,
    ),
    env.DB.prepare("UPDATE rooms SET current_occupancy = current_occupancy + 1, status = 'Occupied', updated_at = CURRENT_TIMESTAMP WHERE id = ?;").bind(roomId),
    env.DB.prepare("UPDATE patients SET current_room_id = ?, status = 'Admitted', updated_at = CURRENT_TIMESTAMP WHERE id = ?;").bind(roomId, patientId),
  ];

  if (bedId) {
    statements.push(env.DB.prepare("UPDATE beds SET status = 'Occupied', updated_at = CURRENT_TIMESTAMP WHERE id = ?;").bind(bedId));
  }

  await env.DB.batch(statements);

  await audit(env, request, "hospitalization created", "hospitalizations", hospitalizationId, `Room ${room.roomNumber}`);
  return ok({ id: hospitalizationId }, 201);
}

async function getDailyReport(env, url) {
  const date = url.searchParams.get("date") || new Date().toISOString().slice(0, 10);
  const patients = await first(env.DB.prepare("SELECT COUNT(*) AS total FROM patients WHERE substr(registration_date, 1, 10) = ?;").bind(date));
  const appointments = await first(env.DB.prepare("SELECT COUNT(*) AS total FROM appointments WHERE date = ?;").bind(date));
  const payments = await first(
    env.DB.prepare(`
      SELECT COALESCE(SUM(total_amount), 0) AS total_amount,
             COALESCE(SUM(paid_amount), 0) AS paid_amount,
             COALESCE(SUM(balance_amount), 0) AS balance_amount,
             COUNT(*) AS payment_count
      FROM payments
      WHERE substr(payment_date, 1, 10) = ?;
    `).bind(date),
  );
  const visits = await first(env.DB.prepare("SELECT COUNT(*) AS total FROM medical_visits WHERE substr(visit_date, 1, 10) = ?;").bind(date));

  return { date, patientsRegistered: patients.total, appointments: appointments.total, visits: visits.total, payments };
}
