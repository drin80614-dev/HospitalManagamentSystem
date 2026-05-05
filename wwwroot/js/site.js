(() => {
  const html = document.documentElement;
  const translations = {
    sq: {
      "Dashboard": "Paneli",
      "Users": "Perdoruesit",
      "Doctors": "Doktoret",
      "Receptionists": "Recepsionistet",
      "Departments": "Departamentet",
      "Services": "Sherbimet",
      "Payments": "Pagesat",
      "Inventory": "Inventari",
      "Reports": "Raportet",
      "My Patients": "Pacientet e mi",
      "My Appointments": "Terminet e mia",
      "Prescription": "Recete",
      "Lab Request": "Kerkese laboratori",
      "Lab Results": "Rezultatet laboratorike",
      "Dental Inventory": "Inventari stomatologjik",
      "Register Patient": "Regjistro pacient",
      "Patients": "Pacientet",
      "Appointment": "Termin",
      "Payment": "Pagese",
      "Appointments": "Terminet",
      "Logout": "Dil",
      "Sign in": "Kycu",
      "Staff access": "Qasje per stafin",
      "Use your hospital account to continue.": "Perdor llogarine e klinikes per te vazhduar.",
      "Use your clinic account to continue.": "Perdor llogarine e klinikes per te vazhduar.",
      "Aesthetic clinic management platform": "Platforme per menaxhimin e klinikes estetike",
      "A modern clinic workspace for admins, doctors, and reception staff.": "Hapesire moderne pune per admina, doktore dhe recepsion.",
      "Aesthetic workflow": "Rrjedha estetike",
      "Dental clinic management platform": "Platforme per menaxhimin e ordinances stomatologjike",
      "A modern dental clinic workspace for admin, dentist, and reception staff.": "Hapesire moderne pune per admin, stomatologe dhe recepsion.",
      "Dental workflow": "Rrjedha stomatologjike",
      "Username or email": "Perdoruesi ose emaili",
      "Password": "Fjalekalimi",
      "Keep me signed in for this shift": "Me mbaj te kycur per kete nderrim",
      "Forgot password?": "Ke harruar fjalekalimin?",
      "Login": "Kycu",
      "Dashboard": "Paneli",
      "Total patients": "Totali i pacienteve",
      "Registered today": "Regjistruar sot",
      "Total doctors": "Totali i doktoreve",
      "Total dentists": "Totali i stomatologeve",
      "Dental services": "Sherbimet stomatologjike",
      "Low stock items": "Artikuj me stok te ulet",
      "Today appointments": "Terminet e sotme",
      "Pending payments": "Pagesat ne pritje",
      "Completed visits": "Vizitat e perfunduara",
      "Recent patients": "Pacientet e fundit",
      "Quick actions": "Veprime te shpejta",
      "Search patients, dentists, appointments": "Kerko paciente, stomatologe, termine",
      "Create appointment": "Krijo termin",
      "Create Appointment": "Krijo termin",
      "Create appointment": "Krijo termin",
      "Schedule": "Orari",
      "Calendar": "Kalendari",
      "Calendar view": "Pamja kalendarike",
      "Appointment calendar": "Kalendari i termineve",
      "Doctor": "Doktori",
      "Patient": "Pacienti",
      "Status": "Statusi",
      "Date": "Data",
      "Reason": "Arsyeja",
      "Update": "Perditeso",
      "Filter": "Filtro",
      "All": "Te gjitha",
      "Create": "Krijo",
      "Edit": "Ndrysho",
      "Save changes": "Ruaj ndryshimet",
      "Cancel": "Anulo",
      "Back": "Kthehu",
      "Back to payments": "Kthehu te pagesat",
      "Register payment": "Regjistro pagese",
      "Payment method": "Menyra e pageses",
      "Amount": "Shuma",
      "Service total": "Totali i sherbimit",
      "Paid now": "Paguar tani",
      "Paid": "Paguar",
      "Remaining": "Mbetur",
      "Remaining balance": "Shuma e mbetur",
      "Financial balance": "Bilanci financiar",
      "Total treatments": "Totali i trajtimeve",
      "All billed services": "Te gjitha sherbimet e faturuara",
      "Money received": "Parate e pranuara",
      "Unpaid balance": "Borxhi i mbetur",
      "Service": "Sherbimi",
      "Receipt": "Fatura",
      "Print receipt": "Printo faturen",
      "Medication inventory": "Inventari stomatologjik",
      "Add medication": "Shto artikull",
      "Medication": "Artikulli",
      "Category": "Kategoria",
      "Stock": "Stoku",
      "Expiry": "Skadimi",
      "Supplier": "Furnitori",
      "Low stock only": "Vetem stok i ulet",
      "Lab tests": "Testet laboratorike",
      "Update lab result": "Perditeso rezultatin laboratorik",
      "Requested": "Kerkuar",
      "In Progress": "Ne proces",
      "Completed": "Perfunduar",
      "Notes": "Shenime",
      "Notifications": "Njoftimet",
      "Mark read": "Sheno si lexuar",
      "My profile": "Profili im",
      "Change password": "Ndrysho fjalekalimin",
      "Current password": "Fjalekalimi aktual",
      "New password": "Fjalekalimi i ri",
      "Confirm new password": "Konfirmo fjalekalimin e ri",
      "Reset password": "Rivendos fjalekalimin",
      "Generate reset link": "Gjenero linkun e resetimit",
      "Back to login": "Kthehu te kyçja",
      "Access denied": "Qasja u refuzua",
      "No appointments found.": "Nuk u gjeten termine.",
      "No medications found.": "Nuk u gjeten barna.",
      "No lab tests found.": "Nuk u gjeten teste laboratorike.",
      "No notifications yet.": "Ende nuk ka njoftime.",
      "Paid": "Paguar",
      "Pending": "Ne pritje",
      "Cancelled": "Anuluar",
      "Scheduled": "Planifikuar",
      "Waiting": "Ne pritje",
      "Active": "Aktiv",
      "Available": "E lire",
      "Occupied": "E zene",
      "Maintenance": "Mirembajtje"
    }
  };

  const applyLanguage = (lang) => {
    const dictionary = translations[lang] || {};
    html.setAttribute("lang", lang === "sq" ? "sq" : "en");
    html.setAttribute("data-hms-lang", lang);
    document.querySelectorAll(".language-toggle").forEach((button) => {
      button.classList.toggle("active", button.dataset.lang === lang);
    });

    document.querySelectorAll("body *").forEach((node) => {
      if (node.children.length || ["SCRIPT", "STYLE", "TEXTAREA", "INPUT", "OPTION"].includes(node.tagName)) {
        return;
      }

      const original = node.dataset.i18nOriginal || node.textContent.trim();
      if (!original) {
        return;
      }

      node.dataset.i18nOriginal = original;
      node.textContent = lang === "en" ? original : (dictionary[original] || original);
    });

    document.querySelectorAll("input[placeholder]").forEach((input) => {
      const original = input.dataset.i18nPlaceholderOriginal || input.getAttribute("placeholder");
      if (!original) {
        return;
      }

      input.dataset.i18nPlaceholderOriginal = original;
      input.setAttribute("placeholder", lang === "en" ? original : (dictionary[original] || original));
    });
  };

  const savedLanguage = localStorage.getItem("hms-language") || "en";
  applyLanguage(savedLanguage);

  document.querySelectorAll(".language-toggle").forEach((button) => {
    button.addEventListener("click", () => {
      const next = button.dataset.lang || "en";
      localStorage.setItem("hms-language", next);
      applyLanguage(next);
    });
  });

  const savedTheme = localStorage.getItem("hms-theme");
  if (savedTheme) {
    html.setAttribute("data-bs-theme", savedTheme);
  }

  document.querySelectorAll("[data-bs-toggle='tooltip']").forEach((el) => {
    new bootstrap.Tooltip(el);
  });

  document.querySelectorAll(".toast").forEach((toastEl) => {
    const toast = new bootstrap.Toast(toastEl, { delay: 4200 });
    toast.show();
  });

  document.getElementById("themeToggle")?.addEventListener("click", () => {
    const next = html.getAttribute("data-bs-theme") === "dark" ? "light" : "dark";
    html.setAttribute("data-bs-theme", next);
    localStorage.setItem("hms-theme", next);
  });

  document.querySelectorAll("form.js-confirm-delete").forEach((form) => {
    form.addEventListener("submit", async (event) => {
      event.preventDefault();
      const result = await Swal.fire({
        title: "Confirm deletion",
        text: "This record will be permanently removed.",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "Delete",
        confirmButtonColor: "#f9735b"
      });
      if (result.isConfirmed) {
        form.submit();
      }
    });
  });

  document.querySelectorAll("[data-service-select]").forEach((select) => {
    select.addEventListener("change", () => {
      const selected = select.options[select.selectedIndex];
      const target = document.querySelector(select.dataset.serviceSelect);
      if (target && selected?.dataset.price) {
        target.value = selected.dataset.price;
      }
    });
  });

  document.querySelectorAll("form[method='post']").forEach((form) => {
    form.addEventListener("input", () => {
      form.dataset.dirty = "true";
    });
    form.addEventListener("change", () => {
      form.dataset.dirty = "true";
    });
    form.addEventListener("submit", () => {
      form.dataset.dirty = "false";
    });
  });

  const liveRefreshPaths = [/^\/(?!Auth\/|Home\/Error|healthz)/i];
  const editPaths = /\/(Create|Edit|Login|Receipt|Print|Profile|Settings|ForgotPassword|ResetPassword|AccessDenied)/i;
  const isLiveRefreshPage = liveRefreshPaths.some((pattern) => pattern.test(window.location.pathname)) &&
    !editPaths.test(window.location.pathname);
  const liveRefreshButton = document.getElementById("liveRefreshButton");

  const canRefreshNow = () => {
    const active = document.activeElement;
    const isTyping = active && ["INPUT", "TEXTAREA", "SELECT"].includes(active.tagName);
    const hasDirtyForm = Boolean(document.querySelector("form[data-dirty='true']"));
    return !document.hidden && !isTyping && !hasDirtyForm && !document.body.classList.contains("modal-open");
  };

  if (isLiveRefreshPage && liveRefreshButton) {
    liveRefreshButton.classList.remove("d-none");
    liveRefreshButton.addEventListener("click", () => window.location.reload());

    let liveVersion = null;
    let pendingLiveUpdate = false;

    const markPendingUpdate = () => {
      pendingLiveUpdate = true;
      liveRefreshButton.classList.remove("btn-outline-success");
      liveRefreshButton.classList.add("btn-success");
      liveRefreshButton.innerHTML = '<i class="bi bi-arrow-repeat me-1"></i>New data';
    };

    const checkLiveVersion = async () => {
      try {
        const response = await fetch("/LiveSync/Version", {
          cache: "no-store",
          headers: { "Accept": "application/json" }
        });

        if (!response.ok) {
          return;
        }

        const payload = await response.json();
        const nextVersion = Number(payload.version || 0);
        if (!nextVersion) {
          return;
        }

        if (liveVersion === null) {
          liveVersion = nextVersion;
          return;
        }

        if (nextVersion > liveVersion) {
          liveVersion = nextVersion;
          if (canRefreshNow()) {
            window.location.reload();
          } else if (!pendingLiveUpdate) {
            markPendingUpdate();
          }
        }
      } catch {
        // Temporary network issues are handled by the next poll.
      }
    };

    checkLiveVersion();
    setInterval(checkLiveVersion, 3000);
  }

  document.querySelectorAll("a, button[type='submit']").forEach((el) => {
    el.addEventListener("click", () => {
      if (!el.closest(".no-loading")) {
        document.body.classList.add("loading-line");
        setTimeout(() => document.body.classList.remove("loading-line"), 1300);
      }
    });
  });
})();
