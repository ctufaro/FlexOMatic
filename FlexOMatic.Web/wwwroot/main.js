// ==== CONFIG ====
const gallery = document.getElementById('gallery');
const form = document.getElementById('cropForm');
const input = document.getElementById('cropInput');
const loader = document.getElementById('loader');

const creditModal = document.getElementById('creditModal');
const robloxNameInput = document.getElementById('robloxNameInput');
const rememberName = document.getElementById('rememberName');
const skipCreditBtn = document.getElementById('skipCreditBtn');
const saveCreditBtn = document.getElementById('saveCreditBtn');

// const API_BASE = 'https://localhost:7261';
const API_BASE = 'https://flexomatic-api-amg6gebfgsg7ewcz.eastus-01.azurewebsites.net';

const endpoints = {
    getCrops: `${API_BASE}/api/flexcrop`,
    generateCrop: `${API_BASE}/api/fleximage/generate`,
    getConfig: `${API_BASE}/api/config`,
    // Optional: if you add a credit endpoint later, wire it here:
    // creditCrop: (id) => `${API_BASE}/api/flexcrop/${id}/credit`
};

let SUBMIT_ENABLED = false;
let lastSubmittedCropId = null; // populate from generate response if available

// ==== RARITY CONFIG ====
const rarityOptions = [
    { name: 'MYTHICAL', weight: 1, badge: '#e11d48', glow: 'hue-rotate(-30deg) saturate(3) brightness(1.4)', bg: 'bg-rose-50' },
    { name: 'LEGENDARY', weight: 3, badge: '#f59e0b', glow: 'hue-rotate(20deg) saturate(2.2) brightness(1.3)', bg: 'bg-yellow-50' },
    { name: 'EPIC', weight: 6, badge: '#9333ea', glow: 'hue-rotate(280deg) saturate(2.2) brightness(1.2)', bg: 'bg-purple-50' },
    { name: 'RARE', weight: 10, badge: '#3b82f6', glow: 'hue-rotate(200deg) saturate(2) brightness(1.1)', bg: 'bg-blue-50' },
    { name: 'UNCOMMON', weight: 20, badge: '#4ade80', glow: 'hue-rotate(140deg) saturate(1.5) brightness(1.1)', bg: 'bg-green-50' },
    { name: 'COMMON', weight: 60, badge: '#d4d4d4', glow: 'saturate(0%) brightness(1)', bg: 'bg-gray-50' }
];

// ==== HELPERS ====
function getRarity(rarity) {
    const key = (rarity || 'COMMON').toUpperCase();
    return rarityOptions.find(r => r.name === key) || rarityOptions.find(r => r.name === 'COMMON');
}

function setSubmitEnabled(enabled) {
    SUBMIT_ENABLED = enabled;
    const btn = form.querySelector("button[type='submit']");
    if (!btn) return;

    if (enabled) {
        btn.disabled = false;
        btn.textContent = "SUBMIT";
        btn.classList.remove("bg-gray-500", "cursor-not-allowed");
        btn.classList.add("bg-green-600", "hover:bg-green-700");
    } else {
        btn.disabled = true;
        btn.textContent = "SUBMIT DISABLED";
        btn.classList.remove("bg-green-600", "hover:bg-green-700");
        btn.classList.add("bg-gray-500", "cursor-not-allowed");
    }
}


function createCropCard(crop) {
    const rarity = getRarity(crop.rarity);

    const card = document.createElement('div');
    card.className = 'bg-white text-black rounded-lg shadow-xl overflow-hidden max-w-sm';

    card.innerHTML = `
    <div class="relative w-full h-60 bg-black">
      <img src="assets/gradient.jpg" class="absolute inset-0 w-full h-full object-cover z-0" style="filter: ${rarity.glow};">
      <span class="absolute top-2 left-2 px-4 py-1 rounded-full text-sm text-white rarity-badge z-10" style="background-color: ${rarity.badge};">${rarity.name}</span>
      <img src="${crop.imageUrl}" alt="Crop Image" class="relative z-10 object-contain mx-auto h-full">
    </div>
    <div class="${rarity.bg} text-black px-4 py-4 space-y-2">
      <h2 class="text-xl font-extrabold text-center"><span class="card-title">${crop.cropName}</span></h2>
      <p class="text-sm text-center italic text-gray-700">${crop.lore || ''}</p>
      <p class="text-center text-sm">Submitted by <span class="font-bold">${crop.submitterName || 'Anonymous'}</span></p>
      <div class="flex justify-center gap-6 text-2xl mt-2">
        <button data-like="${crop.cropID}" class="flex items-center gap-1 text-green-600 hover:scale-110 transition">
            <span>👍</span> ${crop.likes}
        </button>
      </div>
    </div>
  `;
    return card;
}

async function loadCrops() {
    try {
        const res = await fetch(endpoints.getCrops);
        if (!res.ok) throw new Error(`Server returned ${res.status} - ${res.statusText}`);
        const crops = await res.json();

        gallery.innerHTML = '';
        crops.forEach(crop => gallery.appendChild(createCropCard(crop)));
    } catch (err) {
        console.error("Failed to load crops:", err);
        gallery.innerHTML = `<p class="text-white text-lg">🚨 Failed to load crops. Try again later.</p>`;
    }
}

// ==== MODAL CONTROL ====
function openCreditModal(presetName = '') {
    robloxNameInput.value = presetName || '';
    rememberName.checked = !!presetName;
    creditModal.classList.remove('hidden');
    creditModal.classList.add('flex');

    // Restart animation
    const modalBox = creditModal.querySelector('.animate-modalEnter');
    modalBox.classList.remove('animate-modalEnter'); // reset
    void modalBox.offsetWidth; // trigger reflow
    modalBox.classList.add('animate-modalEnter');

    robloxNameInput.focus();
}
function closeCreditModal() {
    creditModal.classList.add('hidden');
    creditModal.classList.remove('flex');
}

function launchConfetti() {
    const canvas = document.getElementById('confettiCanvas');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    const particles = [];
    const colors = ['#facc15', '#f97316', '#4ade80', '#3b82f6', '#ec4899'];

    // Set canvas to header size
    const resizeCanvas = () => {
        canvas.width = canvas.offsetWidth;
        canvas.height = canvas.offsetHeight;
    };
    resizeCanvas();

    for (let i = 0; i < 20; i++) {
        particles.push({
            x: Math.random() * canvas.width,
            y: Math.random() * -20,
            r: 4 + Math.random() * 4,
            c: colors[Math.floor(Math.random() * colors.length)],
            vy: 2 + Math.random() * 2,
            vx: (Math.random() - 0.5) * 2
        });
    }

    let frame;
    function draw() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);
        particles.forEach(p => {
            p.x += p.vx;
            p.y += p.vy;
            ctx.beginPath();
            ctx.arc(p.x, p.y, p.r, 0, Math.PI * 2);
            ctx.fillStyle = p.c;
            ctx.fill();
        });
        frame = requestAnimationFrame(draw);
    }

    draw();

    // Stop after ~1.5 seconds
    setTimeout(() => cancelAnimationFrame(frame), 1500);
}

// ==== SUBMIT HANDLER ====
form.addEventListener('submit', async (e) => {
    e.preventDefault();
    if (!SUBMIT_ENABLED) return;

    const idea = input.value.trim();
    if (!idea) return;

    // Include saved name if we have it
    const savedName = localStorage.getItem('robloxName') || null;

    loader.classList.remove('hidden');
    form.classList.add('opacity-50', 'pointer-events-none');

    try {
        const res = await fetch(endpoints.generateCrop, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            // If backend supports submitterName, it will record it now
            body: JSON.stringify({ cropIdea: idea, submitterName: savedName })
        });

        const data = await res.json();

        lastSubmittedCropId = data.cropId || data.CropID || data.id || null;

        if (res.ok) {
            // Save returned ID if backend provides it (recommended)
            lastSubmittedCropId = data.cropId ?? null;

            input.value = '';

            // Post-submit flow: if we don't have a saved name, offer the credit modal
            if (!savedName) {
                openCreditModal('');
            }

            setTimeout(() => {
                loader.classList.add('hidden');
                form.classList.remove('opacity-50', 'pointer-events-none');
                loadCrops();
            }, 1200);
        } else {
            alert(data.error || 'Something went wrong.');
            throw new Error(data.error);
        }
    } catch (err) {
        console.error("Server error submitting crop:", err);
        alert('Server error submitting crop.');
        loader.classList.add('hidden');
        form.classList.remove('opacity-50', 'pointer-events-none');
    }
});

// Modal buttons
skipCreditBtn.addEventListener('click', () => {
    closeCreditModal();
});

saveCreditBtn.addEventListener('click', async () => {
    const name = robloxNameInput.value.trim();
    if (!name) { closeCreditModal(); return; }

    if (rememberName.checked) localStorage.setItem('robloxName', name);

    if (lastSubmittedCropId) {
        await fetch(`${API_BASE}/api/flexcrop/${lastSubmittedCropId}/credit`, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ submitterName: name })
        });
    }
    closeCreditModal();
    loadCrops();
});

gallery.addEventListener('click', async (e) => {
    const btn = e.target.closest('[data-like]');
    if (!btn) return;
    const cropId = btn.getAttribute('data-like');

    // prevent spamming with session storage
    if (sessionStorage.getItem(`liked_${cropId}`)) return;
    sessionStorage.setItem(`liked_${cropId}`, '1');

    await fetch(`${API_BASE}/api/flexcrop/${cropId}/like`, { method: 'POST' });
    loadCrops();
});

// ==== INIT ====
window.addEventListener('DOMContentLoaded', async () => {
    try {
        const res = await fetch(endpoints.getConfig);
        const config = await res.json();
        setSubmitEnabled(!!config.enableSubmit);
    } catch (err) {
        console.warn("Couldn't fetch /api/config, disabling submit just to be safe.");
        setSubmitEnabled(false);
    }

    // Prefill saved name into credit modal when opened (UX nicety)
    const saved = localStorage.getItem('robloxName');
    if (saved) robloxNameInput.value = saved;

    loadCrops();
});
