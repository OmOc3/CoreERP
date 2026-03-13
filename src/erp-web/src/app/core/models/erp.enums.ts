export interface SelectOption<TValue = number | string> {
  value: TValue;
  label: string;
}

export const purchaseOrderStatusOptions: ReadonlyArray<SelectOption<number>> = [
  { value: 1, label: 'Draft' },
  { value: 2, label: 'Pending approval' },
  { value: 3, label: 'Approved' },
  { value: 4, label: 'Partially received' },
  { value: 5, label: 'Completed' },
  { value: 6, label: 'Cancelled' },
  { value: 7, label: 'Rejected' }
];

export const salesOrderStatusOptions: ReadonlyArray<SelectOption<number>> = [
  { value: 1, label: 'Draft' },
  { value: 2, label: 'Pending approval' },
  { value: 3, label: 'Approved' },
  { value: 4, label: 'Partially delivered' },
  { value: 5, label: 'Completed' },
  { value: 6, label: 'Cancelled' },
  { value: 7, label: 'Rejected' }
];

export const invoiceStatusOptions: ReadonlyArray<SelectOption<number>> = [
  { value: 1, label: 'Draft' },
  { value: 2, label: 'Pending approval' },
  { value: 3, label: 'Posted' },
  { value: 4, label: 'Partially paid' },
  { value: 5, label: 'Paid' },
  { value: 6, label: 'Cancelled' },
  { value: 7, label: 'Rejected' }
];

export const paymentTypeOptions: ReadonlyArray<SelectOption<number>> = [
  { value: 1, label: 'Customer receipt' },
  { value: 2, label: 'Supplier payment' }
];

export const paymentStatusOptions: ReadonlyArray<SelectOption<number>> = [
  { value: 1, label: 'Posted' },
  { value: 2, label: 'Voided' }
];

export const returnTypeOptions: ReadonlyArray<SelectOption<number>> = [
  { value: 1, label: 'Sales return' },
  { value: 2, label: 'Purchase return' }
];

export const returnStatusOptions: ReadonlyArray<SelectOption<number>> = [
  { value: 1, label: 'Draft' },
  { value: 2, label: 'Posted' },
  { value: 3, label: 'Cancelled' }
];

export const inventoryMovementTypeOptions: ReadonlyArray<SelectOption<number>> = [
  { value: 1, label: 'Opening balance' },
  { value: 2, label: 'Purchase receipt' },
  { value: 3, label: 'Sale issue' },
  { value: 4, label: 'Sales return' },
  { value: 5, label: 'Purchase return' },
  { value: 6, label: 'Adjustment increase' },
  { value: 7, label: 'Adjustment decrease' },
  { value: 8, label: 'Transfer in' },
  { value: 9, label: 'Transfer out' }
];

export const approvalStatusOptions: ReadonlyArray<SelectOption<number>> = [
  { value: 1, label: 'Pending' },
  { value: 2, label: 'Approved' },
  { value: 3, label: 'Rejected' }
];

export const approvalDocumentTypeOptions: ReadonlyArray<SelectOption<number>> = [
  { value: 1, label: 'Purchase order' },
  { value: 2, label: 'Sales order' },
  { value: 3, label: 'Purchase invoice' },
  { value: 4, label: 'Sales invoice' }
];

export const alertTypeOptions: ReadonlyArray<SelectOption<number>> = [
  { value: 1, label: 'Low stock' },
  { value: 2, label: 'Workflow' }
];

export function optionLabel<TValue>(options: ReadonlyArray<SelectOption<TValue>>, value: TValue | null | undefined): string {
  return options.find((item) => item.value === value)?.label ?? String(value ?? '');
}
