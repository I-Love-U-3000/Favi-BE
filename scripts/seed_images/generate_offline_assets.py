import os

avatar_dir = r"C:\Users\MINH QUANG\Favi\Favi-BE\Favi-BE\Favi-BE.API\wwwroot\seed-assets\avatars"
cover_dir = r"C:\Users\MINH QUANG\Favi\Favi-BE\Favi-BE\Favi-BE.API\wwwroot\seed-assets\covers"
os.makedirs(avatar_dir, exist_ok=True)
os.makedirs(cover_dir, exist_ok=True)

avatar_colors = [
    ("#6366F1", "#A855F7"), ("#EC4899", "#F43F5E"), ("#3B82F6", "#06B6D4"),
    ("#10B981", "#14B8A6"), ("#F59E0B", "#EF4444"), ("#8B5CF6", "#EC4899"),
    ("#06B6D4", "#3B82F6"), ("#84CC16", "#10B981"), ("#F97316", "#F59E0B"),
    ("#D946EF", "#8B5CF6"), ("#0EA5E9", "#6366F1"), ("#E11D48", "#BE185D"),
    ("#059669", "#047857"), ("#7C3AED", "#4C1D95"), ("#2563EB", "#1D4ED8"),
    ("#DB2777", "#9D174D"), ("#EA580C", "#C2410C"), ("#65A30D", "#4D7C0F"),
    ("#0891B2", "#0E7490"), ("#4F46E5", "#3730A3"), ("#9333EA", "#6B21A8"),
    ("#C026D3", "#86198F"), ("#DC2626", "#991B1B"), ("#16A34A", "#15803D"),
    ("#0284C7", "#0369A1"), ("#7E22CE", "#581C87"), ("#B91C1C", "#7F1D1D"),
    ("#15803D", "#14532D"), ("#1D4ED8", "#1E3A8A"), ("#BE185D", "#831843")
]

for i, (c1, c2) in enumerate(avatar_colors, 1):
    svg = f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 128 128" width="128" height="128">
  <defs>
    <linearGradient id="g{i}" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="{c1}"/>
      <stop offset="100%" stop-color="{c2}"/>
    </linearGradient>
  </defs>
  <circle cx="64" cy="64" r="64" fill="url(#g{i})"/>
  <circle cx="64" cy="50" r="22" fill="#ffffff" fill-opacity="0.9"/>
  <path d="M28 108 C28 88 44 80 64 80 C84 80 100 88 100 108 Z" fill="#ffffff" fill-opacity="0.9"/>
</svg>"""
    with open(os.path.join(avatar_dir, f"avatar_{i}.svg"), "w", encoding="utf-8") as f:
        f.write(svg)

cover_colors = [
    ("#1E1B4B", "#4338CA", "#818CF8"),
    ("#0F172A", "#0284C7", "#38BDF8"),
    ("#064E3B", "#059669", "#34D399"),
    ("#701A75", "#C026D3", "#F472B6"),
    ("#450A0A", "#DC2626", "#F87171"),
    ("#1E293B", "#334155", "#64748B"),
    ("#172554", "#1E40AF", "#60A5FA"),
    ("#365314", "#4D7C0F", "#A3E635"),
    ("#4A044E", "#86198F", "#E879F9"),
    ("#422006", "#B45309", "#FBBF24"),
    ("#164E63", "#0891B2", "#22D3EE"),
    ("#2E1065", "#6D28D9", "#A78BFA"),
    ("#022C22", "#047857", "#6EE7B7"),
    ("#18181B", "#3F3F46", "#71717A"),
    ("#500724", "#BE185D", "#FB7185"),
    ("#1C1917", "#44403C", "#78716C"),
    ("#1E3A8A", "#2563EB", "#93C5FD"),
    ("#14532D", "#15803D", "#4ADE80"),
    ("#581C87", "#7E22CE", "#C084FC"),
    ("#312E81", "#4F46E5", "#A5B4FC")
]

for i, (c1, c2, c3) in enumerate(cover_colors, 1):
    svg = f"""<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1200 400" width="1200" height="400">
  <defs>
    <linearGradient id="cg{i}" x1="0%" y1="0%" x2="100%" y2="100%">
      <stop offset="0%" stop-color="{c1}"/>
      <stop offset="50%" stop-color="{c2}"/>
      <stop offset="100%" stop-color="{c3}"/>
    </linearGradient>
  </defs>
  <rect width="1200" height="400" fill="url(#cg{i})"/>
  <circle cx="200" cy="350" r="180" fill="#ffffff" fill-opacity="0.04"/>
  <circle cx="1050" cy="50" r="240" fill="#ffffff" fill-opacity="0.05"/>
  <path d="M0 320 Q300 240 600 300 T1200 260 L1200 400 L0 400 Z" fill="#ffffff" fill-opacity="0.06"/>
</svg>"""
    with open(os.path.join(cover_dir, f"cover_{i}.svg"), "w", encoding="utf-8") as f:
        f.write(svg)

print("Generated 30 avatars and 20 covers successfully.")
