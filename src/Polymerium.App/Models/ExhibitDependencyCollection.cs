using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Polymerium.App.Models;

public class ExhibitDependencyCollection(IList<ExhibitDependencyModel> items)
    : Collection<ExhibitDependencyModel>(items) { }