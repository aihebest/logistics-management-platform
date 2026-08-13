import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { movementRegisterApi, type MovementSummaryLine } from '../../services/api'
import { PageLoader } from '../../components/ui/LoadingSpinner'
import { format } from 'date-fns'

/**
 * Movement Register — vehicle-grouped summary for vendor and accounts
 * reconciliation.
 *
 * Replaces the hand-maintained Excel sheet: one block per vehicle showing every
 * movement in the period with an auto-calculated distance (Mileage In − Mileage
 * Out) and a per-vehicle total. Columns can be switched off before printing,
 * because the copy sent to accounts doesn't need every operational field.
 */

type ColumnKey =
  | 'date' | 'timeOut' | 'timeIn' | 'purpose' | 'passengers' | 'route' | 'driver'
  | 'ref' | 'gatePass' | 'mileageOut' | 'mileageIn' | 'distance' | 'status'

const COLUMNS: { key: ColumnKey; label: string; numeric?: boolean }[] = [
  { key: 'date',       label: 'Date' },
  { key: 'timeOut',    label: 'Time Out' },
  { key: 'timeIn',     label: 'Time In' },
  { key: 'purpose',    label: 'Purpose' },
  { key: 'passengers', label: 'Passenger(s)' },
  { key: 'route',      label: 'Route' },
  { key: 'driver',     label: 'Driver' },
  { key: 'ref',        label: 'Ref No' },
  { key: 'gatePass',   label: 'Gate Pass' },
  { key: 'mileageOut', label: 'Mileage Out', numeric: true },
  { key: 'mileageIn',  label: 'Mileage In',  numeric: true },
  { key: 'distance',   label: 'Distance (km)', numeric: true },
  { key: 'status',     label: 'Status' },
]

// Sensible default for the accounts copy — operational noise switched off.
const DEFAULT_VISIBLE: ColumnKey[] = [
  'date', 'timeOut', 'timeIn', 'purpose', 'route', 'driver',
  'mileageOut', 'mileageIn', 'distance',
]

const todayStr = () => new Date().toISOString().slice(0, 10)
const monthStartStr = () => {
  const d = new Date()
  return new Date(d.getFullYear(), d.getMonth(), 1).toISOString().slice(0, 10)
}

export default function MovementSummaryPage() {
  const [from, setFrom] = useState(monthStartStr())
  const [to, setTo] = useState(todayStr())
  const [vehicleReg, setVehicleReg] = useState('')
  const [visible, setVisible] = useState<ColumnKey[]>(DEFAULT_VISIBLE)
  const [showColumns, setShowColumns] = useState(false)

  const { data, isLoading, isError, error } = useQuery({
    queryKey: ['movement-summary', from, to, vehicleReg],
    queryFn: () => movementRegisterApi.getSummary({
      from, to, vehicleReg: vehicleReg || undefined,
    }),
  })

  const shown = COLUMNS.filter(c => visible.includes(c.key))

  const toggle = (key: ColumnKey) =>
    setVisible(v => v.includes(key) ? v.filter(k => k !== key) : [...v, key])

  const cell = (m: MovementSummaryLine, key: ColumnKey): string => {
    switch (key) {
      case 'date':       return format(new Date(m.movementDateTime), 'dd MMM yyyy')
      case 'timeOut':    return format(new Date(m.movementDateTime), 'HH:mm')
      case 'timeIn':     return m.returnDateTime ? format(new Date(m.returnDateTime), 'HH:mm') : '—'
      case 'purpose':    return m.purpose
      case 'passengers': return m.passengers ?? '—'
      case 'route':      return `${m.origin} → ${m.destination}`
      case 'driver':     return m.driverName ?? '—'
      case 'ref':        return m.relatedRefNo ?? '—'
      case 'gatePass':   return m.gatePassNo ?? '—'
      case 'mileageOut': return m.mileageOut?.toLocaleString() ?? '—'
      case 'mileageIn':  return m.mileageIn?.toLocaleString() ?? '—'
      case 'distance':   return m.distanceKm != null ? m.distanceKm.toLocaleString() : '—'
      case 'status':     return m.status
    }
  }

  const exportCsv = () => {
    if (!data) return
    const rows: string[][] = []
    rows.push([`Movement Register Summary  ${data.fromDate} to ${data.toDate}`])
    rows.push([])
    for (const v of data.vehicles) {
      rows.push([`Vehicle: ${v.vehicleReg}`])
      rows.push(shown.map(c => c.label))
      for (const m of v.movements) rows.push(shown.map(c => cell(m, c.key)))
      rows.push([`Trips: ${v.tripCount}`, `Total Distance (km): ${v.totalDistanceKm}`])
      rows.push([])
    }
    rows.push([`Grand total distance (km): ${data.grandTotalDistanceKm}`,
               `Total trips: ${data.totalTrips}`,
               `Vehicles: ${data.vehicleCount}`])

    const csv = rows
      .map(r => r.map(f => `"${String(f).replace(/"/g, '""')}"`).join(','))
      .join('\n')
    const blob = new Blob([csv], { type: 'text/csv;charset=utf-8;' })
    const url = URL.createObjectURL(blob)
    const a = document.createElement('a')
    a.href = url
    a.download = `movement-summary_${data.fromDate}_to_${data.toDate}.csv`
    a.click()
    URL.revokeObjectURL(url)
  }

  if (isLoading) return <PageLoader />

  return (
    <div className="space-y-4">
      {/* Print rules: hide chrome, keep each vehicle block intact across pages */}
      <style>{`
        @media print {
          .no-print { display: none !important; }
          .print-block { break-inside: avoid; page-break-inside: avoid; }
          body { background: #fff; }
          table { font-size: 11px; }
        }
      `}</style>

      {/* ── Controls ─────────────────────────────────────────────────────── */}
      <div className="card p-4 no-print space-y-3">
        <div className="flex flex-wrap items-end gap-3">
          <div>
            <label className="label">From</label>
            <input type="date" className="input" value={from} onChange={e => setFrom(e.target.value)} />
          </div>
          <div>
            <label className="label">To</label>
            <input type="date" className="input" value={to} onChange={e => setTo(e.target.value)} />
          </div>
          <div>
            <label className="label">Vehicle (optional)</label>
            <input
              className="input" placeholder="e.g. GGU 693 TX"
              value={vehicleReg} onChange={e => setVehicleReg(e.target.value.toUpperCase())}
            />
          </div>
          <div className="flex gap-2 ml-auto">
            <button className="btn-secondary" onClick={() => setShowColumns(s => !s)}>
              Columns ({shown.length})
            </button>
            <button className="btn-secondary" onClick={exportCsv} disabled={!data}>Export CSV</button>
            <button className="btn-primary" onClick={() => window.print()} disabled={!data}>Print</button>
          </div>
        </div>

        {showColumns && (
          <div className="border-t pt-3">
            <p className="text-xs text-gray-500 mb-2">
              Untick any column you don't want on the printed sheet.
            </p>
            <div className="flex flex-wrap gap-x-5 gap-y-2">
              {COLUMNS.map(c => (
                <label key={c.key} className="flex items-center gap-2 text-sm">
                  <input
                    type="checkbox"
                    checked={visible.includes(c.key)}
                    onChange={() => toggle(c.key)}
                  />
                  {c.label}
                </label>
              ))}
            </div>
          </div>
        )}
      </div>

      {isError && (
        <div className="card p-4 text-sm text-red-700 bg-red-50 border border-red-200">
          Could not load the summary. {(error as Error)?.message ?? ''}
        </div>
      )}

      {/* ── Report header ────────────────────────────────────────────────── */}
      {data && (
        <>
          <div className="card p-5">
            <h1 className="text-lg font-bold text-gray-900">Desicon Engineering — Vehicle Movement Summary</h1>
            <p className="text-sm text-gray-600 mt-1">
              Period: <strong>{data.fromDate}</strong> to <strong>{data.toDate}</strong>
              {vehicleReg && <> · Vehicle: <strong>{vehicleReg}</strong></>}
            </p>
            <div className="flex flex-wrap gap-6 mt-3 text-sm">
              <span>Vehicles: <strong>{data.vehicleCount}</strong></span>
              <span>Total trips: <strong>{data.totalTrips}</strong></span>
              <span>Total distance: <strong>{data.grandTotalDistanceKm.toLocaleString()} km</strong></span>
            </div>
          </div>

          {data.vehicles.length === 0 && (
            <div className="card p-12 text-center text-gray-400">
              No vehicle movements recorded in this period.
            </div>
          )}

          {/* ── One block per vehicle ──────────────────────────────────────── */}
          {data.vehicles.map(v => (
            <div key={v.vehicleReg} className="card p-5 print-block">
              <div className="flex flex-wrap items-baseline justify-between gap-2 mb-3">
                <h2 className="text-base font-bold text-gray-900">{v.vehicleReg}</h2>
                <div className="flex flex-wrap gap-4 text-sm text-gray-600">
                  <span>Trips: <strong>{v.tripCount}</strong></span>
                  {v.openingOdometer != null && (
                    <span>Opening: <strong>{v.openingOdometer.toLocaleString()}</strong></span>
                  )}
                  {v.closingOdometer != null && (
                    <span>Closing: <strong>{v.closingOdometer.toLocaleString()}</strong></span>
                  )}
                  {v.openMovements > 0 && (
                    <span className="text-amber-600">Still out: <strong>{v.openMovements}</strong></span>
                  )}
                </div>
              </div>

              <div className="overflow-x-auto">
                <table className="min-w-full text-sm">
                  <thead className="bg-gray-50">
                    <tr>
                      {shown.map(c => (
                        <th
                          key={c.key}
                          className={`px-3 py-2 text-xs font-medium text-gray-600 uppercase ${
                            c.numeric ? 'text-right' : 'text-left'
                          }`}
                        >
                          {c.label}
                        </th>
                      ))}
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-gray-100">
                    {v.movements.map((m, i) => (
                      <tr key={i}>
                        {shown.map(c => (
                          <td
                            key={c.key}
                            className={`px-3 py-2 whitespace-nowrap text-gray-700 ${
                              c.numeric ? 'text-right tabular-nums' : ''
                            }`}
                          >
                            {cell(m, c.key)}
                          </td>
                        ))}
                      </tr>
                    ))}
                  </tbody>
                  <tfoot>
                    <tr className="bg-gray-50 font-semibold">
                      <td className="px-3 py-2" colSpan={Math.max(shown.length - 1, 1)}>
                        Total distance covered — {v.vehicleReg}
                      </td>
                      <td className="px-3 py-2 text-right tabular-nums">
                        {v.totalDistanceKm.toLocaleString()} km
                      </td>
                    </tr>
                  </tfoot>
                </table>
              </div>
            </div>
          ))}

          {/* ── Sign-off strip for the vendor / accounts copy ──────────────── */}
          <div className="card p-5 print-block">
            <div className="flex justify-between text-lg font-bold border-t-2 border-gray-800 pt-3">
              <span>GRAND TOTAL DISTANCE</span>
              <span className="tabular-nums">{data.grandTotalDistanceKm.toLocaleString()} km</span>
            </div>
            <div className="grid grid-cols-3 gap-8 mt-10 text-xs text-gray-600">
              {['Prepared by', 'Vendor / Station', 'Accounts'].map(role => (
                <div key={role}>
                  <div className="border-t border-gray-400 pt-1">{role}</div>
                  <div className="mt-6 border-t border-gray-400 pt-1">Date</div>
                </div>
              ))}
            </div>
          </div>
        </>
      )}
    </div>
  )
}
