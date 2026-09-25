import { useEffect, useState } from 'react'
import './App.css'

const COMMAND_API = 'http://localhost:5284'
const READ_MODEL_API = 'http://localhost:5183'

const defaultOrderForm = {
  clientName: 'Іван Петренко',
  vehicleMake: 'Toyota',
  vehicleModel: 'Camry',
  vin: 'JT123456789',
  licensePlate: 'BC1234AB',
}

const defaultMechanicForm = {
  mechanicId: '11111111-1111-1111-1111-111111111111',
  mechanicName: 'Олександр Коваль',
}

const defaultWorkForm = {
  workName: 'Заміна мастила',
  hours: '1.5',
  hourlyRate: '600',
}

const defaultPartForm = {
  partName: 'Масляний фільтр',
  quantity: '1',
  unitPrice: '450',
}

const defaultPaymentForm = {
  amount: '1350',
  paymentMethod: 'Card',
}

function App() {
  const [orders, setOrders] = useState([])
  const [selectedOrderId, setSelectedOrderId] = useState(null)
  const [selectedOrder, setSelectedOrder] = useState(null)
  const [works, setWorks] = useState([])
  const [parts, setParts] = useState([])
  const [events, setEvents] = useState([])
  const [loading, setLoading] = useState(false)
  const [orderForm, setOrderForm] = useState(defaultOrderForm)
  const [mechanicForm, setMechanicForm] = useState(defaultMechanicForm)
  const [workForm, setWorkForm] = useState(defaultWorkForm)
  const [partForm, setPartForm] = useState(defaultPartForm)
  const [paymentForm, setPaymentForm] = useState(defaultPaymentForm)

  const refreshOrders = async () => {
    const response = await fetch(`${READ_MODEL_API}/api/orders`)
    const data = await response.json()
    setOrders(data)
    if (!selectedOrderId && data.length > 0) {
      setSelectedOrderId(data[0].id)
    }
  }

  useEffect(() => {
    refreshOrders()
    const interval = setInterval(refreshOrders, 2000)
    return () => clearInterval(interval)
  }, [])

  useEffect(() => {
    if (!selectedOrderId) return

    const loadDetails = async () => {
      const orderResponse = await fetch(`${READ_MODEL_API}/api/orders/${selectedOrderId}`)
      if (orderResponse.ok) {
        const orderData = await orderResponse.json()
        setSelectedOrder(orderData)
      }

      const worksResponse = await fetch(`${READ_MODEL_API}/api/orders/${selectedOrderId}/works`)
      const worksData = await worksResponse.json()
      setWorks(worksData)

      const partsResponse = await fetch(`${READ_MODEL_API}/api/orders/${selectedOrderId}/parts`)
      const partsData = await partsResponse.json()
      setParts(partsData)

      const eventsResponse = await fetch(`${COMMAND_API}/api/orders/${selectedOrderId}/events`)
      if (eventsResponse.ok) {
        const eventsData = await eventsResponse.json()
        setEvents(eventsData)
      }
    }

    loadDetails()
  }, [selectedOrderId])

  const callCommand = async (endpoint, payload, actionName) => {
    setLoading(true)
    try {
      const response = await fetch(`${COMMAND_API}/api/orders${endpoint}`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(payload),
      })

      const result = await response.json().catch(() => ({}))
      if (!response.ok) {
        throw new Error(result.title || result.message || result || `${actionName} failed`)
      }

      await refreshOrders()
      return result
    } finally {
      setLoading(false)
    }
  }

  const handleCreateOrder = async (event) => {
    event.preventDefault()
    await callCommand('', orderForm, 'Create order')
  }

  const handleAssignMechanic = async (event) => {
    event.preventDefault()
    await callCommand(`/${selectedOrderId}/mechanic`, {
      mechanicId: mechanicForm.mechanicId,
      mechanicName: mechanicForm.mechanicName,
    }, 'Assign mechanic')
  }

  const handleAddWork = async (event) => {
    event.preventDefault()
    await callCommand(`/${selectedOrderId}/works`, {
      workName: workForm.workName,
      hours: Number(workForm.hours),
      hourlyRate: Number(workForm.hourlyRate),
    }, 'Add work')
  }

  const handleReservePart = async (event) => {
    event.preventDefault()
    await callCommand(`/${selectedOrderId}/parts`, {
      partName: partForm.partName,
      quantity: Number(partForm.quantity),
      unitPrice: Number(partForm.unitPrice),
    }, 'Reserve part')
  }

  const handlePayment = async (event) => {
    event.preventDefault()
    await callCommand(`/${selectedOrderId}/payment`, {
      amount: Number(paymentForm.amount),
      paymentMethod: paymentForm.paymentMethod,
    }, 'Payment')
  }

  const handleComplete = async () => {
    await callCommand(`/${selectedOrderId}/complete`, {}, 'Complete order')
  }

  return (
    <div className="page-shell">
      <header className="topbar">
        <div>
          <p className="eyebrow">AutoService EventSourcing</p>
          <h1>Service Orders</h1>
        </div>
        <button className="refresh-button" onClick={refreshOrders}>
          Refresh read model
        </button>
      </header>

      <section className="panel create-panel">
        <h2>Create order</h2>
        <form className="inline-form" onSubmit={handleCreateOrder}>
          <input value={orderForm.clientName} onChange={(e) => setOrderForm({ ...orderForm, clientName: e.target.value })} placeholder="Client" />
          <input value={orderForm.vehicleMake} onChange={(e) => setOrderForm({ ...orderForm, vehicleMake: e.target.value })} placeholder="Vehicle make" />
          <input value={orderForm.vehicleModel} onChange={(e) => setOrderForm({ ...orderForm, vehicleModel: e.target.value })} placeholder="Vehicle model" />
          <input value={orderForm.vin} onChange={(e) => setOrderForm({ ...orderForm, vin: e.target.value })} placeholder="VIN" />
          <input value={orderForm.licensePlate} onChange={(e) => setOrderForm({ ...orderForm, licensePlate: e.target.value })} placeholder="License plate" />
          <button type="submit" disabled={loading}>Create</button>
        </form>
      </section>

      <section className="content-grid">
        <div className="panel">
          <h2>Orders</h2>
          {orders.length === 0 ? <p>No service orders yet.</p> : (
            <table>
              <thead>
                <tr>
                  <th>Client</th>
                  <th>Vehicle</th>
                  <th>Mechanic</th>
                  <th>Status</th>
                  <th>Total</th>
                  <th>Payment</th>
                </tr>
              </thead>
              <tbody>
                {orders.map((order) => (
                  <tr key={order.id} className={order.id === selectedOrderId ? 'selected' : ''} onClick={() => setSelectedOrderId(order.id)}>
                    <td>{order.clientName}</td>
                    <td>{order.vehicleMake} {order.vehicleModel}</td>
                    <td>{order.mechanicName || 'Unassigned'}</td>
                    <td>{order.status}</td>
                    <td>{order.totalAmount} USD</td>
                    <td>{order.isPaid ? 'Paid' : 'Pending'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        <div className="panel details-panel">
          {selectedOrder ? (
            <>
              <h2>Order details</h2>
              <div className="details-grid">
                <div><strong>Client:</strong> {selectedOrder.clientName}</div>
                <div><strong>Vehicle:</strong> {selectedOrder.vehicleMake} {selectedOrder.vehicleModel}</div>
                <div><strong>VIN:</strong> {selectedOrder.vin}</div>
                <div><strong>License:</strong> {selectedOrder.licensePlate}</div>
                <div><strong>Mechanic:</strong> {selectedOrder.mechanicName || 'Unassigned'}</div>
                <div><strong>Status:</strong> {selectedOrder.status}</div>
                <div><strong>Total:</strong> {selectedOrder.totalAmount}</div>
                <div><strong>Paid:</strong> {selectedOrder.isPaid ? 'Yes' : 'No'}</div>
              </div>

              <div className="stack-actions">
                <form className="mini-form" onSubmit={handleAssignMechanic}>
                  <input value={mechanicForm.mechanicId} onChange={(e) => setMechanicForm({ ...mechanicForm, mechanicId: e.target.value })} placeholder="Mechanic ID" />
                  <input value={mechanicForm.mechanicName} onChange={(e) => setMechanicForm({ ...mechanicForm, mechanicName: e.target.value })} placeholder="Mechanic name" />
                  <button type="submit" disabled={loading}>Assign mechanic</button>
                </form>

                <form className="mini-form" onSubmit={handleAddWork}>
                  <input value={workForm.workName} onChange={(e) => setWorkForm({ ...workForm, workName: e.target.value })} placeholder="Work name" />
                  <input value={workForm.hours} onChange={(e) => setWorkForm({ ...workForm, hours: e.target.value })} placeholder="Hours" />
                  <input value={workForm.hourlyRate} onChange={(e) => setWorkForm({ ...workForm, hourlyRate: e.target.value })} placeholder="Hourly rate" />
                  <button type="submit" disabled={loading}>Add work</button>
                </form>

                <form className="mini-form" onSubmit={handleReservePart}>
                  <input value={partForm.partName} onChange={(e) => setPartForm({ ...partForm, partName: e.target.value })} placeholder="Part name" />
                  <input value={partForm.quantity} onChange={(e) => setPartForm({ ...partForm, quantity: e.target.value })} placeholder="Qty" />
                  <input value={partForm.unitPrice} onChange={(e) => setPartForm({ ...partForm, unitPrice: e.target.value })} placeholder="Unit price" />
                  <button type="submit" disabled={loading}>Reserve part</button>
                </form>

                <form className="mini-form" onSubmit={handlePayment}>
                  <input value={paymentForm.amount} onChange={(e) => setPaymentForm({ ...paymentForm, amount: e.target.value })} placeholder="Amount" />
                  <input value={paymentForm.paymentMethod} onChange={(e) => setPaymentForm({ ...paymentForm, paymentMethod: e.target.value })} placeholder="Payment method" />
                  <button type="submit" disabled={loading}>Pay</button>
                </form>

                <div className="complete-row">
                  <button className="complete-button" onClick={handleComplete} disabled={loading}>Complete</button>
                </div>
              </div>

              <div className="sub-grid">
                <div>
                  <h3>Works</h3>
                  <ul className="list-box">
                    {works.length === 0 ? <li>None</li> : works.map((item) => <li key={item.id}>{item.workName} — {item.hours}h / {item.hourlyRate}</li>)}
                  </ul>
                </div>
                <div>
                  <h3>Parts</h3>
                  <ul className="list-box">
                    {parts.length === 0 ? <li>None</li> : parts.map((item) => <li key={item.id}>{item.partName} — {item.quantity} x {item.unitPrice}</li>)}
                  </ul>
                </div>
                <div>
                  <h3>Event history</h3>
                  <ul className="list-box">
                    {events.length === 0 ? <li>No events</li> : events.map((event) => <li key={`${event.sequenceNumber}-${event.eventType}`}>{event.sequenceNumber}. {event.eventType}</li>)}
                  </ul>
                </div>
              </div>
            </>
          ) : (
            <p>Select an order.</p>
          )}
        </div>
      </section>
    </div>
  )
}

export default App
