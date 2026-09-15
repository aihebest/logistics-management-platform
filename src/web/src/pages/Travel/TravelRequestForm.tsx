import { format } from 'date-fns'
import type { TravelRequest, TravelLeg } from '../../services/api'

/**
 * Printable replica of DEL-LG-FRM-002 Rev 07.
 *
 * Laid out cell for cell against the paper form so a printed copy is
 * interchangeable with the original. The browser's own print dialog produces
 * the PDF, which keeps the layout exact and needs no PDF engine on the server.
 *
 * The signature boxes are filled from the recorded approvals rather than left
 * blank. An authenticated approval against a named account, timestamped, is
 * stronger evidence than a handwritten signature — and unlike a scribble it can
 * be traced back afterwards.
 */

interface Props {
  request: TravelRequest
  onClose: () => void
}

/** Blank rows keep the printed table the same height as the paper original. */
function padRows(legs: TravelLeg[], direction: string, minRows = 3) {
  const rows = legs.filter(l => l.direction === direction)
  const blanks = Math.max(0, minRows - rows.length)
  return { rows, blanks }
}

/**
 * One of the three signature boxes. Filled from the recorded approval when the
 * stage has been reached, and showing what it is waiting on when it hasn't.
 */
function SignatureBlock({
  name, at, notes, placeholder,
}: {
  name?: string
  at?: string
  notes?: string
  placeholder: string
}) {
  return (
    <td className="trf-cell align-top" style={{ height: '90px', width: '33.33%' }}>
      {name ? (
        <div className="text-[10px] leading-snug">
          <div><span className="text-gray-500">Name:</span> <strong>{name}</strong></div>
          <div className="text-gray-500 mt-0.5">
            Sign: <em>Signed electronically — Desicon Logistics Platform</em>
          </div>
          <div><span className="text-gray-500">Date:</span> {at ? format(new Date(at), 'dd MMM yyyy HH:mm') : '—'}</div>
          {notes && <div className="mt-0.5 text-gray-600">Note: {notes}</div>}
        </div>
      ) : (
        <div className="text-[10px] text-gray-400 italic">{placeholder}</div>
      )}
    </td>
  )
}

export default function TravelRequestForm({ request: r, onClose }: Props) {
  const outbound = padRows(r.legs, 'Outbound')
  const inbound  = padRows(r.legs, 'Inbound')
  const isApproved = r.status === 'Approved'

  const legRow = (l: TravelLeg, i: number) => (
    <tr key={`${l.direction}-${i}`}>
      <td className="trf-cell">{l.travelDate ? format(new Date(l.travelDate), 'dd/MM/yyyy') : ''}</td>
      <td className="trf-cell">{l.from}</td>
      <td className="trf-cell">{l.to}</td>
      <td className="trf-cell">{l.preferredAirline ?? ''}</td>
      <td className="trf-cell">{l.preferredTime ?? ''}</td>
    </tr>
  )

  const blankRow = (key: string) => (
    <tr key={key}>
      {[0, 1, 2, 3, 4].map(c => <td key={c} className="trf-cell">&nbsp;</td>)}
    </tr>
  )

  return (
    <>
      <style>{`
        .trf-cell {
          border: 1px solid #000;
          padding: 3px 5px;
          font-size: 11px;
          vertical-align: middle;
        }
        .trf-label { font-weight: 700; }
        .trf-head {
          border: 1px solid #000;
          padding: 4px;
          font-size: 10px;
          font-weight: 700;
          text-align: center;
          background: #bfbfbf;
        }
        .trf-table { width: 100%; border-collapse: collapse; }

        /* Until the form is fully approved, anything printed carries a clear
           mark so an unapproved copy cannot be walked through as authorised. */
        .trf-draft::before {
          content: attr(data-draft);
          position: absolute;
          top: 42%;
          left: 50%;
          transform: translate(-50%, -50%) rotate(-28deg);
          font-size: 58px;
          font-weight: 800;
          color: rgba(220, 38, 38, 0.16);
          letter-spacing: 3px;
          pointer-events: none;
          white-space: nowrap;
          z-index: 50;
        }

        @media print {
          /* Only the form prints — the app chrome around it is suppressed. */
          body * { visibility: hidden !important; }
          #trf-print, #trf-print * { visibility: visible !important; }
          #trf-print {
            position: absolute;
            left: 0; top: 0;
            width: 100%;
            padding: 0;
            margin: 0;
          }
          .no-print { display: none !important; }
          @page { size: A4 portrait; margin: 12mm; }
        }
      `}</style>

      {/* On-screen wrapper: a preview of exactly what will print. */}
      <div className="no-print fixed inset-0 z-40 bg-black/40 overflow-auto p-6">
        <div className="max-w-4xl mx-auto mb-3 flex items-center justify-between">
          <p className="text-white text-sm">
            Preview — this is exactly what prints. Use your browser's print dialog and
            choose <strong>Save as PDF</strong>.
          </p>
          <div className="flex gap-2">
            <button className="btn-primary text-sm" onClick={() => window.print()}>Print / Save as PDF</button>
            <button className="btn-secondary text-sm" onClick={onClose}>Close</button>
          </div>
        </div>
      </div>

      <div
        id="trf-print"
        className={`fixed inset-0 z-50 overflow-auto bg-white mx-auto my-16 max-w-4xl p-8 shadow-2xl ${isApproved ? '' : 'trf-draft'}`}
        data-draft={isApproved ? '' : 'NOT YET APPROVED'}
        style={{ position: 'relative' }}
      >
        {/* ── Header ──────────────────────────────────────────────────────── */}
        <div className="flex items-start justify-between mb-2">
          <div className="flex items-center gap-2">
            <div className="w-9 h-9 rounded-full bg-[#1F3864] flex items-center justify-center text-white font-bold text-lg">
              D
            </div>
            <span className="text-2xl font-bold tracking-tight text-[#1F3864]">desicon</span>
          </div>
          <div className="text-right text-[11px] font-bold">
            <div>DEL-LG-FRM-002</div>
            <div>Rev 07</div>
          </div>
        </div>

        <h1 className="text-center text-[17px] font-bold text-blue-800 tracking-wide my-3">
          TRAVEL REQUEST FORM
        </h1>

        {/* ── Details ─────────────────────────────────────────────────────── */}
        <table className="trf-table mb-1">
          <tbody>
            <tr>
              <td className="trf-cell trf-label" style={{ width: '28%' }}>Project/Cost Center Code</td>
              <td className="trf-cell" colSpan={3}>{r.projectCostCentreCode ?? ''}</td>
            </tr>
            <tr>
              <td className="trf-cell trf-label">TRF No:</td>
              <td className="trf-cell" style={{ width: '30%' }}><strong>{r.formNumber}</strong></td>
              <td className="trf-cell trf-label" style={{ width: '12%' }}>Date</td>
              <td className="trf-cell">{format(new Date(r.formDate), 'dd/MM/yyyy')}</td>
            </tr>
            <tr>
              <td className="trf-cell trf-label">SURNAME:</td>
              <td className="trf-cell">{r.surname}</td>
              <td className="trf-cell trf-label">GIVEN NAME :</td>
              <td className="trf-cell">{r.givenName}</td>
            </tr>
            <tr>
              <td className="trf-cell trf-label">DEPT:</td>
              <td className="trf-cell">{r.department}</td>
              <td className="trf-cell trf-label">POSITION :</td>
              <td className="trf-cell">{r.position ?? ''}</td>
            </tr>
            <tr>
              <td className="trf-cell trf-label">PHONE No :</td>
              <td className="trf-cell">{r.phoneNumber ?? ''}</td>
              <td className="trf-cell trf-label">EMAIL :</td>
              <td className="trf-cell">{r.email ?? ''}</td>
            </tr>
            <tr>
              <td className="trf-cell trf-label">ADDRESS:</td>
              <td className="trf-cell" colSpan={3}>6B Oko Awo Street, VI Lagos</td>
            </tr>
            <tr>
              <td className="trf-cell trf-label">PURPOSE OF TRAVEL:</td>
              <td className="trf-cell" colSpan={3}>{r.purposeOfTravel}</td>
            </tr>
          </tbody>
        </table>

        {/* ── Routing ─────────────────────────────────────────────────────── */}
        <p className="text-center text-[12px] font-bold my-2">ROUTING REQUIRED</p>

        <p className="text-[11px] font-bold mb-1">OUTBOUND</p>
        <table className="trf-table mb-2">
          <thead>
            <tr>
              <th className="trf-head" style={{ width: '16%' }}>DEPARTURE DATE</th>
              <th className="trf-head" style={{ width: '24%' }}>FROM</th>
              <th className="trf-head" style={{ width: '22%' }}>TO</th>
              <th className="trf-head" style={{ width: '22%' }}>PREFERED AIRLINE</th>
              <th className="trf-head" style={{ width: '16%' }}>PREFERED DEPARTURE TIME</th>
            </tr>
          </thead>
          <tbody>
            {outbound.rows.map(legRow)}
            {Array.from({ length: outbound.blanks }, (_, i) => blankRow(`ob-blank-${i}`))}
          </tbody>
        </table>

        <table className="trf-table mb-2">
          <tbody>
            <tr>
              <td className="trf-cell trf-label" style={{ width: '40%' }}>HOTEL BOOKING REQUIRED?</td>
              <td className="trf-cell" style={{ width: '30%' }}>
                YES <span className="inline-block border border-black w-3 h-3 align-middle ml-1 text-center leading-3 text-[10px]">
                  {r.hotelBookingRequired ? '✓' : ''}
                </span>
              </td>
              <td className="trf-cell">
                NO <span className="inline-block border border-black w-3 h-3 align-middle ml-1 text-center leading-3 text-[10px]">
                  {r.hotelBookingRequired ? '' : '✓'}
                </span>
              </td>
            </tr>
          </tbody>
        </table>

        <p className="text-[11px] font-bold mb-1">INBOUND</p>
        <table className="trf-table mb-2">
          <thead>
            <tr>
              <th className="trf-head" style={{ width: '16%' }}>RETURN DATE</th>
              <th className="trf-head" style={{ width: '24%' }}>FROM</th>
              <th className="trf-head" style={{ width: '22%' }}>TO</th>
              <th className="trf-head" style={{ width: '22%' }}>PREFERED AIRLINE</th>
              <th className="trf-head" style={{ width: '16%' }}>PREFERED RETURN TIME</th>
            </tr>
          </thead>
          <tbody>
            {inbound.rows.map(legRow)}
            {Array.from({ length: inbound.blanks }, (_, i) => blankRow(`ib-blank-${i}`))}
          </tbody>
        </table>

        <table className="trf-table mb-3">
          <tbody>
            <tr>
              <td className="trf-cell trf-label" style={{ width: '34%' }}>
                ANY OTHER INFORMATION/SPECIAL REQUIREMENTS:
              </td>
              <td className="trf-cell">{r.otherInformation ?? ''}</td>
            </tr>
          </tbody>
        </table>

        {/* ── Signatures ──────────────────────────────────────────────────── */}
        <table className="trf-table mb-3">
          <thead>
            <tr>
              <th className="trf-head">Travellers Signature/Date</th>
              <th className="trf-head">Verified by Head of Dept: Name/Sign/Date</th>
              <th className="trf-head">Management Approval: Name/Sign/Date</th>
            </tr>
          </thead>
          <tbody>
            <tr>
              <SignatureBlock
                name={`${r.givenName} ${r.surname}`.trim()}
                at={r.formDate}
                placeholder="Awaiting submission"
              />
              {/* A head of department cannot verify their own travel, so those
                  requests have no verification stage. Once such a request has
                  moved past submission, say so plainly rather than leaving the
                  box reading as an outstanding step. */}
              <SignatureBlock
                name={r.verifiedByName}
                at={r.verifiedAt}
                notes={r.verificationNotes}
                placeholder={
                  r.status === 'PendingVerification'
                    ? 'Awaiting verification by Head of Department'
                    : 'Not required — raised by the Head of Department'
                }
              />
              <SignatureBlock
                name={r.approvedByName}
                at={r.approvedAt}
                notes={r.approvalNotes}
                placeholder="Awaiting management approval"
              />
            </tr>
          </tbody>
        </table>

        {r.rejectionReason && (
          <p className="text-[11px] text-red-700 font-bold mb-2">
            NOT APPROVED — {r.rejectionReason}
          </p>
        )}

        {/* ── Important notice, verbatim from the original ─────────────────── */}
        <div className="text-[10px] text-blue-800 leading-snug">
          <p className="font-bold">IMPORTANT NOTICE</p>
          <p>
            By signing and submitting this form, you agree that the information given
            will be used for the purposes stated in this form.
          </p>
          <p>
            Also, kindly notify Logistics Unit of any changes in travel date 24hrs prior
            to departure.
          </p>
        </div>
      </div>
    </>
  )
}
