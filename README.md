# NetObjectToXmlFileWritter

Exports/Imports custom C# objects to Xml Files.

The main object that will contain the values can be a object, a IEnumerable or a dictionary.

It supports nested types as long as it is a class, a List or a Dictionary.

## How to use

#### Importing/Exporting to a Dictionary
```cs
public class MySetting {
  public string Name { get; set; } = "Hello" 
  public bool Enable { get; set; } = false;

  public static readonly string KeyName = "Name";
  public static Func<XElement, string> KeyValue = k => k.Element(KeyName).Value;
}

string settingsPath = "C:\config.xml"
Dictionary<string, MySetting> mySettingsDictionary;

//Loading
ConfigurationFileManager<MySetting>.Load(settingsPath, MySetting.KeyValue, out mySettingsDictionary);

setting.Enable = true;

//Saving
ConfigurationFileManager<MySetting>.Save(settingsPath, mySettingsDictionary.Values);

//---

MySetting setting;
//Loading
ConfigurationFileManager<MySetting>.Load(settingsPath, out setting);
setting.Name = "New Name"
ConfigurationFileManager<MySetting>.Save(settingsPath, setting);
```

#### Most complex example (with known supported types, non-exaustive list!)
```cs
public class TestSettings {
  public enum CustomEnum { Value1, Value2 }

  public class CustomObject {
    public string MyString { get; set; } = "10";
    public override string ToString() {
      return MyString;
    }
  }

  public Dictionary<CustomEnum, List<CustomObject>> ExampleDicEnumToListCustomObject { get; set; } = new Dictionary<CustomEnum, List<CustomObject>>();
  public Dictionary<CustomEnum, bool> ExampleDicEnumToBool { get; set; } = new Dictionary<CustomEnum, bool>();        
  public Dictionary<CustomEnum, CustomObject> ExampleDicEnumToCustomObject { get; set; } = new Dictionary<CustomEnum, CustomObject>();
  public Dictionary<int, CustomObject> ExampleDicIntToCustomObject { get; set; } = new Dictionary<int, CustomObject>();
  public Dictionary<CustomObject, int> ExampleDicCustomObjectToInt { get; set; } = new Dictionary<CustomObject, int>();
  public Dictionary<string, int> ExampleDicStringToInt { get; set; } = new Dictionary<string, int>();
  public string ExampleString { get; set; } = "Example";
  public CustomEnum ExampleEnum { get; set; } = CustomEnum.Value1;
  public CustomObject ExampleCustomObject { get; set; } = new CustomObject();

  public List<CustomObject> ExampleListCustomObject { get; set; } = new List<CustomObject>();
  public List<string> ExampleListString { get; set; } = new List<string>();

  public TestSettings() {
    ExampleListCustomObject.Add(new CustomObject() { MyString="Hi"});
    ExampleListCustomObject.Add(new CustomObject() { MyString="Hello" });
    ExampleListString.Add("A");
    ExampleListString.Add("B");
    ExampleListString.Add("C");
    ExampleDicStringToInt.Add("D", 1);
    ExampleDicStringToInt.Add("E", 2);
    ExampleDicCustomObjectToInt.Add(new CustomObject() { MyString = "Bye" }, 1);
    ExampleDicCustomObjectToInt.Add(new CustomObject() { MyString = "Goodbye" }, 2);

    ExampleDicIntToCustomObject.Add(1, new CustomObject() { MyString = "Cya" });
    ExampleDicIntToCustomObject.Add(2, new CustomObject() { MyString = "See ya" });

    ExampleDicEnumToCustomObject.Add(CustomEnum.Value1, new CustomObject() { MyString = "Eat" });
    ExampleDicEnumToCustomObject.Add(CustomEnum.Value2, new CustomObject() { MyString = "Drink" });            

    ExampleDicEnumToListCustomObject.Add(CustomEnum.Value1, new List<CustomObject>() {
        new CustomObject { MyString="Coffee"},
        new CustomObject() { MyString="Tea"}
    });

    ExampleDicEnumToListCustomObject.Add(CustomEnum.Value2, new List<CustomObject>() {
        new CustomObject { MyString="Beer"},
        new CustomObject() { MyString="Water"}
    });

    ExampleDicEnumToBool.Add(CustomEnum.Value1, true);
    ExampleDicEnumToBool.Add(CustomEnum.Value2, false);
  }
}
```

Will produce...
```xml
<?xml version="1.0" encoding="utf-8"?>
<root>
  <TestSettings>
    <ExampleDicEnumToListCustomObject>
      <Element0>
        <Key>Value1</Key>
        <Value>
          <Element0>
            <MyString>Coffee</MyString>
          </Element0>
          <Element1>
            <MyString>Tea</MyString>
          </Element1>
        </Value>
      </Element0>
      <Element1>
        <Key>Value2</Key>
        <Value>
          <Element0>
            <MyString>Beer</MyString>
          </Element0>
          <Element1>
            <MyString>Water</MyString>
          </Element1>
        </Value>
      </Element1>
    </ExampleDicEnumToListCustomObject>
    <ExampleDicEnumToBool>
      <Element0>
        <Key>Value1</Key>
        <Value>True</Value>
      </Element0>
      <Element1>
        <Key>Value2</Key>
        <Value>False</Value>
      </Element1>
    </ExampleDicEnumToBool>
    <ExampleDicEnumToCustomObject>
      <Element0>
        <Key>Value1</Key>
        <Value>
          <MyString>Eat</MyString>
        </Value>
      </Element0>
      <Element1>
        <Key>Value2</Key>
        <Value>
          <MyString>Drink</MyString>
        </Value>
      </Element1>
    </ExampleDicEnumToCustomObject>
    <ExampleDicIntToCustomObject>
      <Element0>
        <Key>1</Key>
        <Value>
          <MyString>Cya</MyString>
        </Value>
      </Element0>
      <Element1>
        <Key>2</Key>
        <Value>
          <MyString>See ya</MyString>
        </Value>
      </Element1>
    </ExampleDicIntToCustomObject>
    <ExampleDicCustomObjectToInt>
      <Element0>
        <Key>
          <MyString>Bye</MyString>
        </Key>
        <Value>1</Value>
      </Element0>
      <Element1>
        <Key>
          <MyString>Goodbye</MyString>
        </Key>
        <Value>2</Value>
      </Element1>
    </ExampleDicCustomObjectToInt>
    <ExampleDicStringToInt>
      <Element0>
        <Key>D</Key>
        <Value>1</Value>
      </Element0>
      <Element1>
        <Key>E</Key>
        <Value>2</Value>
      </Element1>
    </ExampleDicStringToInt>
    <ExampleString>Example</ExampleString>
    <ExampleEnum>Value1</ExampleEnum>
    <ExampleCustomObject>
      <MyString>10</MyString>
    </ExampleCustomObject>
    <ExampleListCustomObject>
      <Element0>
        <MyString>Hi</MyString>
      </Element0>
      <Element1>
        <MyString>Hello</MyString>
      </Element1>
    </ExampleListCustomObject>
    <ExampleListString>
      <Element0>A</Element0>
      <Element1>B</Element1>
      <Element2>C</Element2>
    </ExampleListString>
  </TestSettings>
</root>
```
