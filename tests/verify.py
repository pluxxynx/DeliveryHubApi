"""HTTP integration tests and an importable Postman collection. Standard library only."""
import copy, json, pathlib, sys, time, urllib.request, urllib.error
ROOT = pathlib.Path(__file__).resolve().parents[1]
BASE = sys.argv[1] if len(sys.argv)>1 else 'http://127.0.0.1:5080'
delivery = dict(sender=dict(city='Москва',postalCode='101000'),receiver=dict(city='Казань',postalCode='420000'),weightKg=2,lengthCm=20,widthCm=15,heightCm=10,priority='standard',declaredValue=1000)
orders=[dict(id='A',destinationCity='Казань',weightKg=2,readyAt='2026-11-11T08:00:00+03:00'),dict(id='B',destinationCity='Казань',weightKg=3,readyAt='2026-11-11T16:00:00+03:00'),dict(id='C',destinationCity='Казань',weightKg=1,readyAt='2026-11-11T17:00:00+03:00'),dict(id='D',destinationCity='Москва',weightKg=1,readyAt='2026-11-11T09:00:00+03:00')]
batch=dict(orders=orders,maxWaitHours=8)
def change(obj, **kw):
    result=copy.deepcopy(obj); result.update(kw); return result
cases=[
 ('T01 Health','GET','/health',None,200,'pm.expect(j.status).to.eql("ok");'),
 ('T02 Standard delivery','POST','/api/delivery/calculate',delivery,200,'pm.expect(j.selected.carrierName).to.eql("СДЭК"); pm.expect(j.selected.price).to.eql(425); pm.expect(j.selected.days).to.eql(3); pm.expect(j.alternatives.length).to.eql(2); pm.expect(j.weights.price).to.eql(0.4);'),
 ('T03 Express delivery','POST','/api/delivery/calculate',change(delivery,priority='express'),200,'pm.expect(j.selected.carrierName).to.eql("СДЭК");'),
 ('T04 Economy delivery','POST','/api/delivery/calculate',change(delivery,priority='economy'),200,'pm.expect(j.priority).to.eql("economy");'),
 ('T05 Volumetric weight','POST','/api/delivery/calculate',change(delivery,lengthCm=100,widthCm=50,heightCm=50),200,'pm.expect(j.billableWeightKg).to.eql(50); pm.expect(j.selected.price).to.eql(3305);'),
 ('T06 Invalid weight','POST','/api/delivery/calculate',change(delivery,weightKg=-1),400,''),
 ('T07 Invalid priority','POST','/api/delivery/calculate',change(delivery,priority='fast'),400,''),
 ('T08 Missing address','POST','/api/delivery/calculate',change(delivery,sender=None),400,''),
 ('T09 Carrier statistics','GET','/api/delivery/carriers?periodDays=30',None,200,'pm.expect(j.carriers.length).to.eql(3); pm.expect(j.carriers[0].ordersCount).to.eql(30); pm.expect(j.dataSource).to.eql("educational-fixtures");'),
 ('T10 Unknown city','GET','/api/delivery/carriers?periodDays=30&city=Unknown',None,200,'pm.expect(j.carriers[0].ordersCount).to.eql(0); pm.expect(j.carriers[0].onTimeRate).to.eql(null);'),
 ('T11 Invalid period','GET','/api/delivery/carriers?periodDays=0',None,400,''),
 ('T12 Batch boundary and cities','POST','/api/batch/optimize',batch,200,'pm.expect(j.batches.length).to.eql(3); pm.expect(j.totalSavings).to.eql(200); pm.expect(j.batches[0].shipmentIds).to.eql(["A","B"]); pm.expect(j.batches[0].batchCost).to.eql(400);'),
 ('T13 Read saved batches','GET','/api/batch',None,200,'pm.expect(j.some(x=>x.shipmentIds.includes("A") && x.shipmentIds.includes("B"))).to.eql(true);'),
 ('T14 Empty orders','POST','/api/batch/optimize',change(batch,orders=[]),400,''),
 ('T15 Duplicate IDs','POST','/api/batch/optimize',change(batch,orders=[orders[0],orders[0]]),400,''),
 ('T16 Invalid wait','POST','/api/batch/optimize',change(batch,maxWaitHours=-1),400,''),
 ('T17 Missing readiness','POST','/api/batch/optimize',change(batch,orders=[{k:v for k,v in orders[0].items() if k!='readyAt'}]),400,''),
 ('T18 Unknown endpoint','GET','/api/missing',None,404,''),
 ('T19 Wrong method','PUT','/api/delivery/calculate',delivery,405,''),
 ('T20 Null order','POST','/api/batch/optimize',change(batch,orders=[None]),400,''),
 ('T21 Malformed JSON','POST','/api/delivery/calculate','{broken',400,''),
 ('T22 Same city','POST','/api/delivery/calculate',change(delivery,receiver=delivery['sender']),200,'pm.expect(j.selected.price).to.eql(341); pm.expect(j.selected.days).to.eql(2);'),
 ('T23 Separate dispatches','POST','/api/batch/optimize',change(batch,maxWaitHours=0),200,'pm.expect(j.batches.length).to.eql(4); pm.expect(j.totalSavings).to.eql(0);'),
 ('T24 Invalid postal code','POST','/api/delivery/calculate',change(delivery,sender=dict(city='Москва',postalCode='abc')),400,''),
]
results=[]; items=[]
for name,method,path,body,status,assertions in cases:
    raw=body if isinstance(body,str) else json.dumps(body,ensure_ascii=False) if body is not None else None
    req=urllib.request.Request(BASE+path,data=raw.encode() if raw is not None else None,headers={'Content-Type':'application/json'},method=method)
    started=time.perf_counter()
    try: response=urllib.request.urlopen(req,timeout=10)
    except urllib.error.HTTPError as e: response=e
    text=response.read().decode(); elapsed=round((time.perf_counter()-started)*1000,2)
    try: data=json.loads(text)
    except ValueError: data=text
    results.append(dict(name=name,method=method,path=path,input=body,expectedStatus=status,actualStatus=response.status,elapsedMs=elapsed,response=data,passed=response.status==status and elapsed<2000))
    script=[f'pm.test("HTTP {status}",()=>pm.response.to.have.status({status}));','pm.test("Response under 2000 ms",()=>pm.expect(pm.response.responseTime).to.be.below(2000));']
    if assertions: script+=['pm.test("Business result",()=>{const j=pm.response.json(); '+assertions+'});']
    request=dict(method=method,header=[dict(key='Content-Type',value='application/json')],url='{{baseUrl}}'+path)
    if raw is not None: request['body']=dict(mode='raw',raw=raw,options=dict(raw=dict(language='json')))
    items.append(dict(name=name,request=request,event=[dict(listen='test',script=dict(type='text/javascript',exec=script))]))
# Independently assert key calculated results, not just HTTP success.
byid={r['name'].split()[0]:r for r in results}
checks={
 'T02':lambda j:j['selected']['price']==425 and j['selected']['days']==3 and j['selected']['carrierName']=='СДЭК' and j['weights']['price']==.4,
 'T05':lambda j:j['billableWeightKg']==50 and j['selected']['price']==3305,
 'T09':lambda j:len(j['carriers'])==3 and all(c['ordersCount']==30 for c in j['carriers']),
 'T10':lambda j:all(c['ordersCount']==0 and c['onTimeRate'] is None for c in j['carriers']),
 'T12':lambda j:len(j['batches'])==3 and j['totalSavings']==200 and j['batches'][0]['shipmentIds']==['A','B'] and j['batches'][0]['batchCost']==400,
 'T13':lambda j:any(b['shipmentIds']==['A','B'] for b in j),
 'T22':lambda j:j['selected']['price']==341 and j['selected']['days']==2,
 'T23':lambda j:len(j['batches'])==4 and j['totalSavings']==0,
}
for key,check in checks.items():
    r=byid[key]
    try: r['businessCheck']=check(r['response'])
    except Exception: r['businessCheck']=False
    r['passed']=r['passed'] and r['businessCheck']
out=ROOT/'artifacts';out.mkdir(exist_ok=True)
(out/'http-results.json').write_text(json.dumps(dict(baseUrl=BASE,results=results),ensure_ascii=False,indent=2),encoding='utf-8')
collection=dict(info=dict(name='Delivery Hub ВПК 3–4',schema='https://schema.getpostman.com/json/collection/v2.1.0/collection.json'),variable=[dict(key='baseUrl',value=BASE,type='string')],item=items)
(ROOT/'DeliveryHub.postman_collection.json').write_text(json.dumps(collection,ensure_ascii=False,indent=2),encoding='utf-8')
print(json.dumps(dict(passed=sum(r['passed'] for r in results),total=len(results),failed=[r['name'] for r in results if not r['passed']],maxMs=max(r['elapsedMs'] for r in results)),ensure_ascii=False))
sys.exit(0 if all(r['passed'] for r in results) else 1)
